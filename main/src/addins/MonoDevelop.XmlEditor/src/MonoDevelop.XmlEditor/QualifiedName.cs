//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//

using System;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// An <see cref="XmlQualifiedName"/> with the namespace prefix.
	/// </summary>
	/// <remarks>
	/// The namespace prefix active for a namespace is 
	/// needed when an element is inserted via autocompletion. This
	/// class just adds this extra information alongside the 
	/// <see cref="XmlQualifiedName"/>.
	/// </remarks>
	public class QualifiedName
	{
		XmlQualifiedName xmlQualifiedName = XmlQualifiedName.Empty;
		string prefix = String.Empty;
		
		public QualifiedName()
		{
		}
		
		public QualifiedName(string name, string namespaceUri)
			: this(name, namespaceUri, String.Empty)
		{
		}
		
		public QualifiedName(string name, string namespaceUri, string prefix)
		{
			xmlQualifiedName = new XmlQualifiedName(name, namespaceUri);
			this.prefix = prefix;
		}
		
		public static bool operator ==(QualifiedName lhs, QualifiedName rhs)
		{
			bool equals = false;
			
			if (((object)lhs != null) && ((object)rhs != null)) {
				equals = lhs.Equals(rhs);
			} else if (((object)lhs == null) && ((object)rhs == null)) {
				equals = true;
			}
			
			return equals;
		}
		
		public static bool operator !=(QualifiedName lhs, QualifiedName rhs)
		{
			return !(lhs == rhs);
		}		
		
		/// <summary>
		/// A qualified name is considered equal if the namespace and 
		/// name are the same.  The prefix is ignored.
		/// </summary>
		public override bool Equals(object obj) 
		{
			bool equals = false;
			
			QualifiedName qualifiedName = obj as QualifiedName;
			if (qualifiedName != null) {
				equals = xmlQualifiedName.Equals(qualifiedName.xmlQualifiedName);
			} else {
				XmlQualifiedName name = obj as XmlQualifiedName;
				if (name != null) {
					equals = xmlQualifiedName.Equals(name);
				}
			}
			
			return equals;
		}
		
		public override int GetHashCode() 
		{
			return xmlQualifiedName.GetHashCode();
		}
		
		/// <summary>
		/// Gets the namespace of the qualified name.
		/// </summary>
		public string Namespace {
			get {
				return xmlQualifiedName.Namespace;
			}
			set {
				xmlQualifiedName = new XmlQualifiedName(xmlQualifiedName.Name, value);
			}
		}
		
		/// <summary>
		/// Gets the name of the element.
		/// </summary>
		public string Name {
			get {
				return xmlQualifiedName.Name;
			}
			set {
				xmlQualifiedName = new XmlQualifiedName(value, xmlQualifiedName.Namespace);
			}
		}
		
		/// <summary>
		/// Gets the namespace prefix used.
		/// </summary>
		public string Prefix {
			get {
				return prefix;
			}
			set {
				prefix = value;
			}
		}
	}
}
