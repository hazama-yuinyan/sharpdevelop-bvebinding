﻿/*
 * Created by SharpDevelop.
 * User: Ryouta
 * Date: 2013/02/13
 * Time: 14:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using BVE5Language.Ast;
using BVE5Language.Parser;
using BVE5Language.Resolver;
using BVE5Language.Semantics;
using BVE5Language.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace BVE5Language
{
	/// <summary>
	/// Description of ResolverTest.
	/// </summary>
	[TestFixture]
	public class ResolverTest
	{
		private ICompilation compilation;
		private IProjectContent project;
		
		[SetUp]
		public void Setup()
		{
			project = new BVE5ProjectContent().AddAssemblyReferences(BVEBuiltins.GetBuiltinAssembly());
			compilation = project.CreateCompilation();
		}
		
		private IEnumerable<TextLocation> FindDollarSigns(string code)
		{
			int line = 1, col = 1;
			foreach(char c in code){
				if(c == '$'){
					yield return new TextLocation(line, col);
				}else if(c == '\n'){
					line++;
					col = 1;
				}else{
					col++;
				}
			}
		}
		
		private Tuple<BVE5AstResolver, AstNode> PrepareResolver(string code)
		{
			var tree = new BVE5RouteFileParser().Parse(code.Replace("$", ""), "<string>");
			var dollars = FindDollarSigns(code).ToArray();
			Assert.AreEqual(2, dollars.Length, "Expected 2 dollar signs marking start+end of desired node");
			
			Setup();
			
			var unresolved_file = tree.ToTypeSystem();
			project = project.AddOrUpdateFiles(unresolved_file);
			compilation = project.CreateCompilation();
			
			var resolver = new BVE5AstResolver(compilation as BVE5Compilation, tree, unresolved_file);
			return Tuple.Create(resolver, tree.FindNode((node) => node.StartLocation == dollars[0] && node.EndLocation == dollars[1]));
		}
		
		private ResolveResult Resolve(string code)
		{
			var prep = PrepareResolver(code);
			Debug.WriteLine(new string('=', 70));
			Debug.WriteLine("Starting new resolver for " + prep.Item2.GetText());
			
			ResolveResult rr = prep.Item1.Resolve(prep.Item2);
			Assert.IsNotNull(rr, "ResolveResult is null - did something go wrong while navigating to the target node?");
			Debug.WriteLine("ResolveResult is " + rr);
			return rr;
		}
		
		private T Resolve<T>(string code) where T : ResolveResult
		{
			ResolveResult rr = Resolve(code);
			Assert.IsNotNull(rr);
			Assert.IsTrue(rr.GetType() == typeof(T), "Resolve should be " + typeof(T).Name + ", but was " + rr.GetType().Name);
			return (T)rr;
		}
		
		[TestCase]
		public void BuiltinType()
		{
			var rr = Resolve<TypeResolveResult>("$Sound$.Load(sound.txt);");
			Assert.AreEqual("global.Sound", rr.Type.FullName);
			Assert.IsFalse(rr.IsError);
		}
		
		[TestCase]
		public void MethodCall()
		{
			var rr = Resolve<InvocationResolveResult>("Sound.$Load$(sound.txt);");
			Assert.AreEqual("global.Sound.Load", rr.Member.FullName);
			Assert.AreEqual("global.void", rr.Type.FullName);
		}
		
		[TestCase]
		public void PositionStatement()
		{
			var rr = Resolve<ConstantResolveResult>("$1000$;");
			Assert.AreEqual("1000", rr.ConstantValue);
			Assert.IsFalse(rr.IsError);
		}
		
		// TODO: implement it
		[TestCase]
		public void ReferenceIndexer()
		{
			
		}
	}
}