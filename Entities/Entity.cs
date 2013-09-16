﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeltaEngine.Content;
using DeltaEngine.Datatypes;

namespace DeltaEngine.Entities
{
	/// <summary>
	/// Each entity has a name, unique components for data and behaviors for logic attached to them.
	/// Entities are used for all engine objects, rendering, game objects, ui, physics, etc.
	/// </summary>
	public abstract class Entity
	{
		protected Entity(List<object> createFromComponents)
			: this()
		{
			foreach (var component in createFromComponents)
				if (!(component is Rectangle) && !(component is Visibility))
					components.Add(component);
		}

		/// <summary>
		/// Entities start out active and are automatically added to the current EntitiesRunner. Call
		/// IsActive to activate or deactivate one. To disable UpdateBehaviors use <see cref="Stop{T}"/>
		/// </summary>
		protected Entity()
		{
			if (EntitiesRunner.Current == null)
				throw new UnableToCreateEntityWithoutInitializedResolverAndEntitiesRunner();
			EntitiesRunner.Current.Add(this);
		}

		public class UnableToCreateEntityWithoutInitializedResolverAndEntitiesRunner : Exception {}

		public virtual bool IsActive
		{
			get { return isActive; }
			set
			{
				if (isActive != value)
					if (value)
						Activate();
					else
						Inactivate();
			}
		}

		private bool isActive = true;

		private void Activate()
		{
			isActive = true;
			EntitiesRunner.Current.Add(this);
		}

		protected void Inactivate()
		{
			isActive = false;
			EntitiesRunner.Current.Remove(this);
		}

		protected readonly List<object> components = new List<object>();

		/// <summary>
		/// Gets a specific component, derived classes can return faster cached values (e.g. Entity2D)
		/// </summary>
		public virtual T Get<T>()
		{
			foreach (T component in components.OfType<T>())
				return component;
			throw new ComponentNotFound(typeof(T));
		}

		public class ComponentNotFound : Exception
		{
			public ComponentNotFound(Type component)
				: base(component.ToString()) {}
		}

		public T GetOrDefault<T>(T defaultValue)
		{
			return Contains<T>() ? Get<T>() : defaultValue;
		}

		public virtual bool Contains<T>()
		{
			return components.OfType<T>().Any();
		}

		public virtual Entity Add<T>(T component)
		{
			EntitiesRunner.Current.CheckIfInUpdateState();
			if (component == null)
				throw new ArgumentNullException();
			if (component is UpdateBehavior)
				throw new InstantiatedHandlerAddedToEntity();
			if (Contains<T>())
				throw new ComponentOfTheSameTypeAddedMoreThanOnce();
			components.Add(component);
			return this;
		}

		public class InstantiatedHandlerAddedToEntity : Exception {}

		public class ComponentOfTheSameTypeAddedMoreThanOnce : Exception {}

		public virtual void Set<T>(T component)
		{
			EntitiesRunner.Current.CheckIfInUpdateState();
			if (component == null)
				throw new ArgumentNullException();
			for (int index = 0; index < components.Count; index++)
				if (components[index] is T)
				{
					components[index] = component;
					return;
				}
			components.Add(component);
		}

		public void Remove<T>()
		{
			components.RemoveAll(c => c is T);
		}

		public int NumberOfComponents
		{
			get { return components.Count; }
		}

		public Entity Start<T>() where T : UpdateBehavior
		{
			var behavior = EntitiesRunner.Current.GetUpdateBehavior<T>();
			if (activeBehaviors.Contains(behavior))
				return this;
			activeBehaviors.Add(behavior);
			EntitiesRunner.Current.AddToUpdateBehavior(behavior, this);
			return this;
		}

		private readonly List<UpdateBehavior> activeBehaviors = new List<UpdateBehavior>();

		public void Stop<T>() where T : UpdateBehavior
		{
			var behavior = EntitiesRunner.Current.GetUpdateBehavior<T>();
			if (!activeBehaviors.Contains(behavior))
				return;
			activeBehaviors.Remove(behavior);
			EntitiesRunner.Current.RemoveFromBehaviorList(behavior, this);
		}

		internal bool ContainsActiveBehavior(UpdateBehavior updateBehavior)
		{
			return activeBehaviors.Contains(updateBehavior);
		}

		internal bool ContainsBehavior<T>() where T : UpdateBehavior
		{
			return activeBehaviors.Any(behavior => behavior.GetType() == typeof(T));
		}

		public Priority UpdatePriority
		{
			get { return updatePriority; }
			set
			{
				if (updatePriority != value)
					EntitiesRunner.Current.ChangeEntityPriority(this, value);
				updatePriority = value;
			}
		}
		private Priority updatePriority = Priority.Normal;

		public void AddTag(string tag)
		{
			if (tags.Contains(tag))
				return;
			tags.Add(tag);
			if (IsActive)
				EntitiesRunner.Current.AddTag(this, tag);
		}

		internal readonly List<string> tags = new List<string>();

		public void RemoveTag(string tag)
		{
			EntitiesRunner.Current.RemoveTag(this, tag);
			tags.Remove(tag);
		}

		public void ClearTags()
		{
			foreach (string tag in tags)
				EntitiesRunner.Current.RemoveTag(this, tag);
			tags.Clear();
		}

		public bool ContainsTag(string tag)
		{
			return tags.Contains(tag);
		}

		public List<string> GetTags()
		{
			return tags;
		}

		public override string ToString()
		{
			return (IsActive ? "" : "<Inactive> ") + GetType().Name +
				(tags.Count > 0 ? " Tags=" + string.Join(", ", tags) : "") +
				(components.Count > 0 ? ": " + GetTypesText(components) : "") +
				(activeBehaviors.Count == 0 ? "" : " [" + GetTypesText(activeBehaviors) + "]");
		}

		private static string GetTypesText<T>(IEnumerable<T> typesList)
		{
			if (typeof(T) == typeof(Type))
				return string.Join(", ", typesList.Select(GetTypeName));
			return string.Join(", ", typesList.Select(GetTypeNameWithValue));
		}

		private static string GetTypeName<T>(T component)
		{
			var type = (component as Type);
			if (typeof(IList).IsAssignableFrom(type) && !type.IsArray)
				return type.Name.Replace("`1", "") + "<" + GetTypesText(type.GetGenericArguments()) + ">";
			return type.Name;
		}

		private static string GetTypeNameWithValue<T>(T instance)
		{
			return GetTypeName(instance.GetType()) +
				(instance.GetType().IsValueType || instance is ContentData ? "=" + instance : "");
		}

		protected internal virtual List<object> GetComponentsForSaving()
		{
			return components;
		}
	}
}