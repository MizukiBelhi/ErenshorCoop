using ErenshorCoop.Client;
using ErenshorCoop.Server;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ErenshorCoop.Modules
{
	public class ModuleMngr
	{
		private List<Module> _loadedModules = new();
		//private Dictionary<PacketType, Module> _packetHandlers = new();
		private Dictionary<Type, string> _moduleHashes = new();
		private Dictionary<PacketType, List<Module>> _packetHandlers = new();


		private enum ModuleLoadState
		{
			LOADED,
			_ALREADY_LOADED,
			FAILED
		}
		public ModuleMngr()
		{
			//Start coroutine to detect new modules every 10 seconds
			Task.Run(async () =>
			{
				while (true)
				{
					await Task.Delay(10000);
					DetectAndLoadNewModules();
				}
			});
		}

		public static string GetModuleILHash(Type moduleType)
		{
			var sb = new StringBuilder();
			sb.Append(moduleType.FullName);

			foreach (var field in moduleType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				sb.Append(field.Name).Append(':').Append(field.FieldType.FullName).Append(';');
			foreach (var prop in moduleType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				sb.Append(prop.Name).Append(':').Append(prop.PropertyType.FullName).Append(';');

			foreach (var method in moduleType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				sb.Append(method.Name).Append('(');
				sb.Append(string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName)));
				sb.Append(')').Append("->").Append(method.ReturnType.FullName).Append(';');

				try
				{
					var body = method.GetMethodBody();
					if (body != null)
					{
						var il = body.GetILAsByteArray();
						if (il != null && il.Length > 0)
							sb.Append(Convert.ToBase64String(il));
					}
				}
				catch{}
			}

			using var sha256 = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(sb.ToString());
			var hash = sha256.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}

		public void AddPacketHandler(PacketType packetType, Module module)
		{

			if (!_packetHandlers.ContainsKey(packetType))
				_packetHandlers.Add(packetType, new());
			if(!_packetHandlers[packetType].Contains(module))
				_packetHandlers[packetType].Add(module);
		}

		public void GatherModules()
		{
			var modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
				.Where(x => x.IsClass
					&& !x.IsAbstract
					&& x.IsSubclassOf(typeof(Module))
					&& Attribute.IsDefined(x, typeof(ModuleAttribute))
					&& !x.IsNested
					&& !x.IsGenericTypeDefinition
					&& !x.Name.StartsWith("<")) // exclude compiler-generated
				.ToList();
			foreach (var moduleType in modules)
			{
				if (LoadModule(moduleType) == ModuleLoadState.FAILED)
				{
					
					continue; //Already loaded or failed to load
				}
				_moduleHashes[moduleType] = GetModuleILHash(moduleType);
			}
		}

		//We need to detect during runtime if a new module has been added and load it
		public void DetectAndLoadNewModules()
		{
			//Logging.Log("Detecting new modules...");
			var modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
				.Where(x => x.IsClass
					&& !x.IsAbstract
					&& x.IsSubclassOf(typeof(Module))
					&& Attribute.IsDefined(x, typeof(ModuleAttribute))
					&& !x.IsNested
					&& !x.IsGenericTypeDefinition
					&& !x.Name.StartsWith("<")) // exclude compiler-generated
				.ToList();
			foreach (var moduleType in modules)
			{
				var currentHash = GetModuleILHash(moduleType);
				if (_moduleHashes.TryGetValue(moduleType, out var previousHash) && previousHash == currentHash)
				{
					//Logging.Log($"Module {moduleType.Name} unchanged, skipping. prevHash: {previousHash} currHash: {currentHash}");
					continue; //Skip trying to load again, same hash as last failure
				}
				try
				{
					if (LoadModule(moduleType) == ModuleLoadState.FAILED)
					{

						continue; //Already loaded or failed to load
					}
				}
				catch(Exception ex)
				{
					Logging.LogError($"Loading module failed {moduleType.Name}: {ex}");
					continue;
				}
				_moduleHashes[moduleType] = currentHash;
			}
			//Detect removed modules that we couldn't cleanup
			foreach(var loadedModule in _loadedModules.ToList())
			{
				if(!modules.Any(m => m == loadedModule.GetType()))
				{
					try { loadedModule.OnCleanup(); }
					catch (Exception ex) { Logging.LogError($"Cleaning up module {loadedModule.GetType().Name}: {ex}"); }

					_loadedModules.Remove(loadedModule);
					//Unregister packet handlers
					foreach (var key in _packetHandlers.Keys.ToList())
					{
						var handlers = _packetHandlers[key];
						if (handlers.Remove(loadedModule) && handlers.Count == 0)
						{
							PacketManager.UnregisterPacket(key);
							_packetHandlers.Remove(key);
						}
					}

					var attr = (ModuleAttribute)Attribute.GetCustomAttribute(loadedModule.GetType(), typeof(ModuleAttribute));
					if (attr != null)
					{
						Logging.Log($"Dynamically Unloaded Module: {attr.Name}");
					}
				}
			}
			//Remove null modules and packet handlers
			_loadedModules = _loadedModules.Where(m => m != null).ToList();
			//Unregister null packet handlers
			foreach(var kvp in _packetHandlers.Where(kvp => kvp.Value == null).ToList())
			{
				PacketManager.UnregisterPacket(kvp.Key);
			}
			_packetHandlers = _packetHandlers.Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		private ModuleLoadState LoadModule(Type moduleType)
		{
			var registerMethod = moduleType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).FirstOrDefault(m => m.Name == "RegisterPacket" && m.IsGenericMethod);
			var baseMethod = typeof(Module).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).First(m => m.Name == "RegisterPacket" && m.IsGenericMethod);

			if (registerMethod.DeclaringType != baseMethod.DeclaringType)
			{
				Logging.LogError($"Do not override or hide RegisterPacket in {moduleType.Name}!");
				return ModuleLoadState.FAILED;
			}

			if (_loadedModules.Any(m => m.GetType() == moduleType))
			{
				var oldModule = _loadedModules.First(m => m.GetType() == moduleType);
				try { oldModule.OnCleanup(); }
				catch (Exception ex) { Logging.LogError($"Cleaning up old module {oldModule.GetType().Name}: {ex}"); }
				_loadedModules.Remove(oldModule);
				//Unregister packet handlers
				foreach (var key in _packetHandlers.Keys.ToList())
				{
					var handlers = _packetHandlers[key];
					if (handlers.Remove(oldModule) && handlers.Count == 0)
					{
						PacketManager.UnregisterPacket(key);
						_packetHandlers.Remove(key);
					}
				}
				Logging.Log($"Unloaded old {moduleType.Name}");
			}


			var moduleInstance = (Module)Activator.CreateInstance(moduleType);
			try { moduleInstance.OnLoad(); }
			catch (Exception ex) { Logging.LogError($"ModuleOnLoad {moduleType.Name}: {ex.Message}"); return ModuleLoadState.FAILED; }

			_loadedModules.Add(moduleInstance);
			// Log the loaded module name from the attribute
			var attr = (ModuleAttribute)Attribute.GetCustomAttribute(moduleType, typeof(ModuleAttribute));
			if (attr != null)
			{
				Logging.Log($"Loaded Module: {attr.Name}");
			}
			else
			{
				Logging.Log($"Loaded Module: {moduleType.Name} (no attribute)");
			}
			return ModuleLoadState.LOADED;
		}

		public void CleanupModules()
		{
			foreach (var module in _loadedModules)
			{
				try { module.OnCleanup(); }
				catch (Exception ex) { Logging.LogError($"Cleaning up module {module.GetType().Name}: {ex}"); }
				//Unregister packet handlers
				foreach (var key in _packetHandlers.Keys.ToList())
				{
					var handlers = _packetHandlers[key];
					if (handlers.Remove(module) && handlers.Count == 0)
					{
						PacketManager.UnregisterPacket(key);
						_packetHandlers.Remove(key);
					}
				}
			}
			_loadedModules.Clear();
		}

		public (T, PacketType) DispatchClientPacket<T>(T packet, PacketType packetType) where T : BasePacket
		{
			//We need to get the module that handles this packet type and call it
			if(_packetHandlers.TryGetValue(packetType, out var handlerList))
			{
				//make sure handlerList is not null
				if (handlerList != null)
				{
					foreach (var handlerModule in handlerList)
					{
						try { return handlerModule.OnReceiveClientPacket(packet, packetType); }
						catch (Exception ex) { Logging.LogError($"OnReceiveClientPacket {handlerModule.GetType().Name}: {ex}"); }
					}
				}
			}

			return (null, 0);
		}
		
		public (T, PacketType) DispatchServerPacket<T>(T packet, PacketType packetType) where T : BasePacket
		{
			//We need to get the module that handles this packet type and call it
			if (_packetHandlers.TryGetValue(packetType, out var handlerList))
			{
				//make sure handlerList is not null
				if (handlerList != null)
				{
					foreach (var handlerModule in handlerList)
					{
						try { return handlerModule.OnReceiveServerPacket(packet, packetType); }
						catch (Exception ex) { Logging.LogError($"OnReceiveServerPacket {handlerModule.GetType().Name}: {ex}"); }
					}
				}
			}

			return (null, 0);
		}
	}

	public class Module
	{
		public bool IsConnected => ClientConnectionManager.Instance?.IsRunning ?? false;
		public bool IsHost => ServerConnectionManager.Instance?.IsRunning ?? false;
		public virtual void OnLoad() { }
		public virtual void OnCleanup() { }
		public virtual (T, PacketType) OnReceiveClientPacket<T>(T packet, PacketType packetType) where T : BasePacket { return (null, 0); }
		public virtual (T, PacketType) OnReceiveServerPacket<T>(T packet, PacketType packetType) where T : BasePacket { return (null, 0); }

		public void RegisterPacket<T>(PacketType type, bool isServerPacket, byte channel) where T : BasePacket, new()
		{
			PacketManager.RegisterPacket<T>(type, isServerPacket, channel);
			ErenshorCoopMod.moduleMngr?.AddPacketHandler(type, this);
		}
	}

	public class ModuleAttribute : Attribute
	{
		public string Name { get; }
		public ModuleAttribute(string name)
		{
			Name = name;
		}
	}
}