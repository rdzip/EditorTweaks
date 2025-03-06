using System;
using System.Reflection;

namespace EditorTweaks.Utils
{
	public static class ReflectionTools
	{
		public static object Call(this object obj, string name)
		{
			return Call(obj, name, null, null);
		}

		public static object Call(this object obj, string name, object[] parameters)
		{
			return Call(obj, name, null, parameters);
		}

		public static object Call(this object obj, string name, Type[] types, object[] parameters)
		{
			var method = types == null ?
				obj.GetType().GetMethod(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) :
				obj.GetType().GetMethod(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, types, null);
			return method.Invoke(obj, parameters);
		}

		public static T Call<T>(this object obj, string name)
		{
			return (T)Call(obj, name, null, null);
		}

		public static T Call<T>(this object obj, string name, object[] parameters)
		{
			return (T)Call(obj, name, null, parameters);
		}

		public static T Call<T>(this object obj, string name, Type[] types, object[] parameters)
		{
			return (T)Call(obj, name, types, parameters);
		}

		public static object Call(Type type, string name)
		{
			return Call(type, name, null, null);
		}

		public static object Call(Type type, string name, object[] parameters)
		{
			return Call(type, name, null, parameters);
		}

		public static object Call(Type type, string name, Type[] types, object[] parameters)
		{
			var method = types == null ?
				type.GetMethod(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) :
				type.GetMethod(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				null, types, null);
			return method.Invoke(null, parameters);
		}

		public static T Call<T>(Type type, string name)
		{
			return (T)Call(type, name, null, null);
		}

		public static T Call<T>(Type type, string name, object[] parameters)
		{
			return (T)Call(type, name, null, parameters);
		}

		public static T Call<T>(Type type, string name, Type[] types, object[] parameters)
		{
			return (T)Call(type, name, types, parameters);
		}

		public static object Get(this object obj, string name)
		{
			Type type = obj.GetType();
			FieldInfo field = type.GetField(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null) return field.GetValue(obj);
			PropertyInfo property = type.GetProperty(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (property != null) return property.GetValue(obj);
			return null;
		}

		public static T Get<T>(this object obj, string name)
		{
			return (T)Get(obj, name);
		}

		public static object Get(Type type, string name)
		{
			FieldInfo field = type.GetField(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (field != null) return field.GetValue(null);
			PropertyInfo property = type.GetProperty(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (property != null) return property.GetValue(null);
			return null;
		}

		public static T Get<T>(Type type, string name)
		{
			return (T)Get(type, name);
		}

		public static void Set(this object obj, string name, object value)
		{
			Type type = obj.GetType();
			FieldInfo field = type.GetField(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null)
			{
				field.SetValue(obj, value);
				return;
			}
			PropertyInfo property = type.GetProperty(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (property != null)
			{
				property.SetValue(obj, value);
				return;
			}
		}

		public static void Set<T>(this object obj, string name, T value)
		{
			Set(obj, name, (object)value);
		}

		public static void Set(Type type, string name, object value)
		{
			FieldInfo field = type.GetField(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (field != null)
			{
				field.SetValue(null, value);
				return;
			}
			PropertyInfo property = type.GetProperty(name,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (property != null)
			{
				property.SetValue(null, value);
				return;
			}
		}

		public static void Set<T>(Type type, string name, T value)
		{
			Set(type, name, (object)value);
		}
	}
}
