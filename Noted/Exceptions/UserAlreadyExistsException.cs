using System;
namespace Noted.Exceptions
{
	public class EntityAlreadyExistsException<T> : Exception
	{
		public EntityAlreadyExistsException(string existing) : base($"{typeof(T)} {existing} already exists.") { }
    }
}

