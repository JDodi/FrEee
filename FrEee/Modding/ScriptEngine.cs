﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using FrEee.Utility.Extensions;
using IronPython.Hosting;
using IronPython.Compiler;
using Microsoft.Scripting.Hosting;
using System.Reflection;
using System.Data;
using FrEee.Utility;
using Microsoft.Scripting.Runtime;

namespace FrEee.Modding
{
	/// <summary>
	/// IronPython scripting engine
	/// </summary>
	public class ScriptEngine : MarshalByRefObject
	{
		/// <summary>
		/// Creates the IronPython scripting engine
		/// </summary>
		static ScriptEngine()
		{
			// http://msdn.microsoft.com/en-us/library/bb763046.aspx
			// http://grokbase.com/t/python/ironpython-users/123kjgw8k8/passing-python-exceptions-in-a-sandboxed-domain

			//Setting the AppDomainSetup. It is very important to set the ApplicationBase to a folder 
			//other than the one in which the sandboxer resides.
			AppDomainSetup adSetup = new AppDomainSetup();
			adSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
			adSetup.ApplicationName = "FrEee";
			adSetup.DynamicBase = "ScriptEngine";

			//Setting the permissions for the AppDomain. We give the permission to execute and to 
			//read/discover the location where the untrusted code is loaded.
			var evidence = new Evidence();
			evidence.AddHostEvidence(new Zone(SecurityZone.MyComputer));
			var permissions = SecurityManager.GetStandardSandbox(evidence);
			var reflection = new ReflectionPermission(PermissionState.Unrestricted);
			permissions.AddPermission(reflection);

			//Now we have everything we need to create the AppDomain, so let's create it.
			sandbox = AppDomain.CreateDomain("ScriptEngine", null, adSetup, permissions, AppDomain.CurrentDomain.GetAssemblies().Select(a => a.Evidence.GetHostEvidence<StrongName>()).Where(sn => sn != null).ToArray());
			engine = Python.CreateEngine(sandbox);
		}

		/// <summary>
		/// Compiles a script.
		/// </summary>
		/// <returns></returns>
		public static CompiledCode Compile(Script script)
		{
			var compiledScript = engine.CreateScriptSourceFromString(script.FullText);
			return compiledScript.Compile();
		}

		/// <summary>
		/// Compiles and caches a script, or returns the cached script if it exists.
		/// </summary>
		/// <param name="source">The script to search for.</param>
		/// <returns>The compiled code.</returns>
		private static CompiledCode GetCompiledScript(Script source)
		{
			if (!compiledScripts.ContainsKey(source))
				compiledScripts.Add(source, Compile(source));
			return compiledScripts[source];
		}

		/// <summary>
		/// Creates a script for some code, or returns the cached scripts if it exists.
		/// </summary>
		/// <param name="sc">Information about the code.</param>
		/// <returns>The script.</returns>
		private static Script GetCodeScript(ScriptCode source)
		{
			if (!codeScripts.ContainsKey(source))
				codeScripts.Add(source, new Script(source.ModuleName, source.Code, source.ExternalScripts));
			return codeScripts[source];
		}

		/// <summary>
		/// Creates a script scope containing user defined variables.
		/// </summary>
		/// <param name="variables">The variables to set, or null to set no variables.</param>
		/// <returns></returns>
		public static ScriptScope CreateScope(IDictionary<string, object> variables = null, IDictionary<string, object> readOnlyVariables = null)
		{
			// Set injected variables
			var scope = engine.CreateScope();
			if (variables != null)
			{
				foreach (var kvp in SerializeScriptVariables(variables))
					scope.SetVariable("_" + kvp.Key, kvp.Value);
			}
			if (readOnlyVariables != null)
			{
				foreach (var kvp in SerializeScriptVariables(readOnlyVariables))
					scope.SetVariable("_" + kvp.Key, kvp.Value);
			}
			return scope;
		}

		public static IDictionary<string, string> SerializeScriptVariables(IDictionary<string, object> variables)
		{
			return variables.Select(kvp => new KeyValuePair<string, string>(kvp.Key, Serializer.SerializeToString(kvp.Value))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		public static IDictionary<string, object> RetrieveVariablesFromScope(ScriptScope scope, IEnumerable<string> variableNames)
		{
			var dict = new Dictionary<string, object>();
			foreach (var varName in variableNames)
				dict.Add(varName, Serializer.DeserializeFromString(scope.GetVariable<string>("_" + varName)));
			return dict;
		}

		/// <summary>
		/// Evaluates a script expression in a sandboxed environment.
		/// Note that the return value of the script may still contain insecure code, so be careful!
		/// Expressions will have the following modules/classes imported:
		/// * System.Linq;
		/// * FrEee.Utility.Extensions
		/// * Math (from System)
		/// * modGlobalScript
		/// </summary>
		/// <param name="expression">The script code to run.</param>
		/// <param name="variables">Variables to inject into the script.</param>
		/// <returns>Any .</returns>
		public static T EvaluateExpression<T>(string expression, IDictionary<string, object> variables = null)
		{
			if (expression.IndexOf("\n") >= 0)
				throw new ScriptException("Cannot evaluate a script containing newlines. Consider using CallFunction instead.");

			var imports = new List<string>();
			imports.Add("import clr;");
			imports.Add("import System;");
			imports.Add("clr.AddReference('System.Core');");
			imports.Add("from System import Linq;");
			imports.Add("clr.AddReferenceToFileAndPath('FrEee.Core.dll');");
			imports.Add("from FrEee.Utility import Extensions;");
			imports.Add("clr.ImportExtensions(Extensions);");
			imports.Add("from System import Math;");

			var deserializers = new List<string>();
			if (variables != null)
			{
				deserializers.Add("from FrEee.Utility import Serializer;");
				foreach (var variable in variables.Keys)
					deserializers.Add(variable + " = Serializer.DeserializeFromString(_" + variable + ");");
			}

			string code;
			ScriptCode sc;
			if (Mod.Current == null || Mod.Current.GlobalScript == null)
			{
				code = string.Join("\n", imports.ToArray()) + "\n" + string.Join("\n", deserializers.ToArray()) + "\n" + expression;
				sc = new ScriptCode("expression", code);
			}
			else
			{
				imports.Add("import " + Mod.Current.GlobalScript.ModuleName + ";");
				code = string.Join("\n", imports.ToArray()) + "\n" + expression;
				sc = new ScriptCode("expression", code, Mod.Current.GlobalScript);
			}
			var script = GetCodeScript(sc);
			var compiledScript = GetCompiledScript(script);
			var scope = CreateScope(variables);
			try
			{
				return (T)compiledScript.Execute(scope);
			}
			catch (Exception ex)
			{
				if (ex.Data.Values.Count > 0)
				{
					dynamic info = ex.Data.Values.Cast<dynamic>().First();
					var debugInfo = info[0].DebugInfo;
					if (debugInfo != null)
					{
						int startLine = debugInfo.StartLine;
						int endLine = debugInfo.StartLine;
						throw new ScriptException(ex, string.Join("\n", script.FullText.Split('\n').Skip(startLine - 1).Take(endLine - startLine + 1).ToArray()));
					}
					else
						throw new ScriptException(ex, "(unknown code)");
				}
				else
					throw new ScriptException(ex, "(unknown code)");
			}
		}

		/// <summary>
		/// Runs a script in a sandboxed environment.
		/// </summary>
		/// <param name="script">The script code to run.</param>
		/// <param name="variables">Read/write variables to inject into the script.</param>
		/// /// <param name="variables">Read-only variables to inject into the script.</param>
		public static void RunScript(Script script, IDictionary<string, object> variables = null, IDictionary<string, object> readOnlyVariables = null)
		{
			var scope = CreateScope(variables, readOnlyVariables);
			var deserializers = new List<string>();
			var serializers = new List<string>();
			if (variables != null)
			{
				deserializers.Add("import clr;");
				deserializers.Add("clr.AddReferenceToFileAndPath('FrEee.Core.dll');");
				deserializers.Add("from FrEee.Utility import Serializer;");
				foreach (var variable in variables.Keys)
				{
					deserializers.Add(variable + " = Serializer.DeserializeFromString(_" + variable + ");");
					serializers.Add("_" + variable + " = Serializer.SerializeToString(" + variable + ");");
				}
				foreach (var variable in readOnlyVariables.Keys)
					deserializers.Add(variable + " = Serializer.DeserializeFromString(_" + variable + ");");
			}
			var code =
				string.Join("\n", deserializers.ToArray()) + "\n" +
				script.Text + "\n" +
				string.Join("\n", serializers.ToArray());
			var sc = new ScriptCode("runner", code, script.ExternalScripts.ToArray());
			var runner = GetCodeScript(sc);
			var compiledScript = GetCompiledScript(runner);
			try
			{
				compiledScript.Execute(scope);
				if (variables != null)
				{
					var newvals = RetrieveVariablesFromScope(scope, variables.Keys);
					foreach (var kvp in variables)
						newvals[kvp.Key].CopyTo(kvp.Value);
				}
			}
			catch (Exception ex)
			{
				if (ex.Data.Values.Count > 0)
				{
					dynamic info = ex.Data.Values.Cast<dynamic>().First();
					var debugInfo = info[0].DebugInfo;
					if (debugInfo != null)
					{
						int startLine = debugInfo.StartLine;
						int endLine = debugInfo.StartLine;
						throw new ScriptException(ex, string.Join("\n", runner.FullText.Split('\n').Skip(startLine - 1).Take(endLine - startLine + 1).ToArray()));
					}
					else
						throw new ScriptException(ex, "(unknown code)");
				}
				else
					throw new ScriptException(ex, "(unknown code)");
			}
		}

		/// <summary>
		/// Calls a script function in a sandboxed environment.
		/// Note that the return value of the script may still contain insecure code, so be careful!
		/// </summary>
		/// <param name="script">The script containing the function.</param>
		/// <param name="function">The name of the function.</param>
		/// <param name="args">Arguments to pass to the function.</param>
		/// <returns>The return value.</returns>
		public static T CallFunction<T>(Script script, string function, params object[] args)
		{
			var deserializers = new List<string>();
			if (args != null)
			{
				deserializers.Add("import clr;");
				deserializers.Add("clr.AddReferenceToFileAndPath('FrEee.Core.dll');");
				deserializers.Add("from FrEee.Utility import Serializer;");
				for (int i = 0; i < args.Length; i++)
					deserializers.Add("arg" + i + " = Serializer.DeserializeFromString(_arg" + i + ");");
				// TODO - serializers so the objects can be modified by the script
			}
			var arglist = new List<string>();
			for (var i = 0; i < args.Length; i++)
				arglist.Add("arg" + i);
			var code = string.Join("\n", deserializers.ToArray()) + "\n" + script.ModuleName + "." + function + "(" + string.Join(", ", arglist) + ")";
			var scope = CreateScope(args.ToDictionary(arg => "arg" + args.IndexOf(arg)));
			var sc = new ScriptCode("functionCall", code, new Script[] { script }.Concat(script.ExternalScripts).ToArray());
			var functionCall = GetCodeScript(sc);
			var compiledScript = GetCompiledScript(functionCall);
			try
			{
				return compiledScript.Execute<T>(scope);
			}
			catch (Exception ex)
			{
				if (ex.Data.Values.Count > 0)
				{
					dynamic info = ex.Data.Values.Cast<dynamic>().First();
					var debugInfo = info[0].DebugInfo;
					if (debugInfo != null)
					{
						int startLine = debugInfo.StartLine;
						int endLine = debugInfo.StartLine;
						throw new ScriptException(ex, string.Join("\n", functionCall.FullText.Split('\n').Skip(startLine - 1).Take(endLine - startLine + 1).ToArray()));
					}
					else
						throw new ScriptException(ex, "(unknown code)");
				}
				else
					throw new ScriptException(ex, "(unknown code)");
			}
		}

		/// <summary>
		/// Calls a script subroutine in a sandboxed environment.
		/// Note that the return value of the script may still contain insecure code, so be careful!
		/// </summary>
		/// <param name="script">The script containing the function.</param>
		/// <param name="function">The name of the function.</param>
		/// <param name="args">Arguments to pass to the function.</param>
		/// <returns>The return value.</returns>
		public static void CallSubroutine(Script script, string function, params object[] args)
		{
			var deserializers = new List<string>();
			if (args != null)
			{
				deserializers.Add("import clr;");
				deserializers.Add("clr.AddReferenceToFileAndPath('FrEee.Core.dll');");
				deserializers.Add("from FrEee.Utility import Serializer;");
				for (int i = 0; i < args.Length; i++)
					deserializers.Add("arg" + i + " = Serializer.DeserializeFromString(_arg" + i + ");");
				// TODO - serializers so the objects can be modified by the script
			}
			var arglist = new List<string>();
			for (var i = 0; i < args.Length; i++)
				arglist.Add("arg" + i);
			var code = string.Join("\n", deserializers.ToArray()) + "\n" + script.ModuleName + "." + function + "(" + string.Join(", ", arglist) + ")";
			var scope = CreateScope(args.ToDictionary(arg => "arg" + args.IndexOf(arg)));
			var sc = new ScriptCode("runner", code, new Script[] { script }.Concat(script.ExternalScripts).ToArray());
			var subCall = GetCodeScript(sc);
			var compiledScript = GetCompiledScript(subCall);

			try
			{
				compiledScript.Execute(scope);
			}
			catch (Exception ex)
			{
				if (ex.Data.Values.Count > 0)
				{
					dynamic info = ex.Data.Values.Cast<dynamic>().First();
					var debugInfo = info[0].DebugInfo;
					if (debugInfo != null)
					{
						int startLine = debugInfo.StartLine;
						int endLine = debugInfo.StartLine;
						throw new ScriptException(ex, string.Join("\n", subCall.FullText.Split('\n').Skip(startLine - 1).Take(endLine - startLine + 1).ToArray()));
					}
					else
						throw new ScriptException(ex, "(unknown code)");
				}
				else
					throw new ScriptException(ex, "(unknown code)");
			}
		}

		private static AppDomain sandbox;
		private static Microsoft.Scripting.Hosting.ScriptEngine engine;

		private static IDictionary<Script, CompiledCode> compiledScripts = new Dictionary<Script, CompiledCode>();
		private static IDictionary<ScriptCode, Script> codeScripts = new Dictionary<ScriptCode, Script>();

		private class ScriptCode
		{
			public ScriptCode(string moduleName, string code, params Script[] externalScripts)
			{
				ModuleName = moduleName;
				Code = code;
				ExternalScripts = externalScripts;
			}

			public string ModuleName { get; set; }
			public string Code { get; set; }
			public Script[] ExternalScripts { get; set; }

			public static bool operator ==(ScriptCode sc1, ScriptCode sc2)
			{
				if (sc1.IsNull() && sc2.IsNull())
					return true;
				if (sc1.IsNull() || sc2.IsNull())
					return false;
				return sc1.ModuleName == sc2.ModuleName && sc1.Code == sc2.Code && sc1.ExternalScripts.SequenceEqual(sc2.ExternalScripts);
			}

			public static bool operator !=(ScriptCode sc1, ScriptCode sc2)
			{
				return !(sc1 == sc2);
			}

			public override bool Equals(object obj)
			{
				if (obj is ScriptCode)
				{
					var sc = (ScriptCode)obj;
					return sc == this;
				}
				return false;
			}

			public override int GetHashCode()
			{
				var hash = ModuleName.GetHashCode() ^ Code.GetHashCode();
				foreach (var xs in ExternalScripts)
					hash ^= xs.GetHashCode();
				return hash;
			}
		}
	}

	public class ScriptException : Exception
	{
		public ScriptException(string message)
		{
			this.message = message;
		}

		public ScriptException(Exception ex, string code)
		{
			if (ex is TargetInvocationException)
				message = "In this code:\n" + code + "\n" + ex.InnerException.Message;
			else
				message = "In this code:\n" + code + "\n" + ex.Message;
		}

		private string message;

		public override string Message
		{
			get
			{
				return message;
			}
		}
	}
}
