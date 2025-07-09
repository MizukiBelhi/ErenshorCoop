using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ErenshorCoop.UI
{
	public interface IPanel
	{
		public GameObject CreatePanel(Transform parent, float width, float height, Canvas canvas=null);
	}

	//For later cleanup
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class UIPanelAttribute : Attribute
	{
		public string Name { get; }

		public UIPanelAttribute(string name)
		{
			Name = name;
		}
	}
}
