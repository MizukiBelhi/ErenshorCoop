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
using System.Threading;
using System.Threading.Tasks;

namespace ErenshorCoop.Modules
{
	public class ModuleMngr : IDisposable
	{
		private List<Module> _loadedModules = new();
		//private Dictionary<PacketType, Module> _packetHandlers = new();
		private Dictionary<Type, string> _moduleHashes = new();
		private Dictionary<PacketType, List<Module>> _packetHandlers = new();

		private CancellationTokenSource cts = new();
		public enum ModuleLoadState
		{
			LOADED,
			_ALREADY_LOADED,
			FAILED
		}
		public ModuleMngr()
		{
		}

		public void Dispose()
		{
			cts.Cancel();
			CleanupModules();
		}

		public bool RegisterModule<T>() where T : Module, new()
		{
			var moduleType = typeof(T);
			if (LoadModule(moduleType) == ModuleLoadState.FAILED)
				return false;
			return true;
		}

		public void UnloadModule(Module m)
		{
			if (_loadedModules.Contains(m))
			{
				try { m.OnCleanup(); }
				catch (Exception ex) { Logging.LogError($"Cleaning up module {m.GetType().Name}: {ex}"); }
				_loadedModules.Remove(m);
				//Unregister packet handlers
				foreach (var key in _packetHandlers.Keys.ToList())
				{
					var handlers = _packetHandlers[key];
					if (handlers.Remove(m) && handlers.Count == 0)
					{
						PacketManager.UnregisterPacket(key);
						_packetHandlers.Remove(key);
					}
				}
				Logging.Log($"Unloaded module: {m.GetType().Name}");
			}
		}
		

		public void AddPacketHandler(PacketType packetType, Module module)
		{

			if (!_packetHandlers.ContainsKey(packetType))
				_packetHandlers.Add(packetType, new());
			if(!_packetHandlers[packetType].Contains(module))
				_packetHandlers[packetType].Add(module);
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