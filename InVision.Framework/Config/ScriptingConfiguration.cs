﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using InVision.Framework.Scripting;

namespace InVision.Framework.Config
{
	[Serializable]
	public class ScriptingConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptingConfiguration"/> class.
		/// </summary>
		public ScriptingConfiguration()
		{
			ScriptManagers = new List<Type>();
		}

		/// <summary>
		/// Gets or sets the execution mode.
		/// </summary>
		/// <value>The execution mode.</value>
		[XmlAttribute("mode")]
		public ExecutionMode ExecutionMode { get; set; }

		/// <summary>
		/// Gets or sets the script managers.
		/// </summary>
		/// <value>The script managers.</value>
		[XmlArray("script-managers")]
		[XmlArrayItem("script-manager")]
		public string[] ScriptManagerTypeNames
		{
			get { return ScriptManagers.Select(t => t.AssemblyQualifiedName).ToArray(); }
			set
			{
				ScriptManagers.Clear();

				foreach (string typename in value) {
					Type type = Type.GetType(typename, true);

					AddScriptManagerType(type);
				}
			}
		}

		/// <summary>
		/// Gets the script managers.
		/// </summary>
		/// <value>The script managers.</value>
		[XmlIgnore]
		public IList<Type> ScriptManagers { get; private set; }

		/// <summary>
		/// Adds the type of the script manager.
		/// </summary>
		/// <param name="type">The type.</param>
		public void AddScriptManagerType(Type type)
		{
			if (!type.IsAbstract && !type.IsInterface &&
				typeof(IScriptManager).IsAssignableFrom(type) &&
				!ScriptManagers.Contains(type)) {
				ScriptManagers.Add(type);
			}
		}
	}
}