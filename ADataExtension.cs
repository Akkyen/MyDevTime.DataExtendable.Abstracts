using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MyDevTime.DataExtendable.Interfaces;
using MyDevTime.ExceptionsWithReturnCode;

namespace MyDevTime.DataExtendable.Abstracts;

/// <summary>
/// Abstract base class representing a data extension that can be extended with additional data extensions.
/// This class implements the <see cref="IDataExtension"/> interface and provides mechanisms for managing 
/// a set of extensions of the type <typeparamref name="T"/>, where <typeparamref name="T"/> must implement 
/// <see cref="IDataExtension"/>.
/// </summary>
/// <typeparam name="T">The type of data extension, which must implement <see cref="IDataExtension"/>.</typeparam>
public abstract class ADataExtension<T> : IDataExtension 
    where T : IDataExtension
{
    #region DataExtension - Fields

    /// <summary>
    /// The unique identifier of this data extension.
    /// </summary>
    private readonly string _extensionId;
    
    /// <summary>
    /// The parent Extendable of this Extension.
    /// Might be another Extension or the root.
    /// </summary>
    private readonly ADataExtendable<T> _parent;
    
    /// <summary>
    /// The base datatype instance.
    /// </summary>
    private readonly ADataExtendable<T> _root;
    

    /// <summary>
    /// A set of extensions of type <typeparamref name="T"/>.
    /// </summary>
    private ISet<T> _extensions;
    
    /// <summary>
    /// A lock to manage read and write access in this class
    /// </summary>
    private ReaderWriterLockSlim _rwls;

    #endregion
    
    
    #region DataExtension - Properties
    
    /// <summary>
    /// Gets the unique identifier of this data extension.
    /// </summary>
    public string ExtensionId => _extensionId;
    
    /// <summary>
    /// Gets the parent Extendable of this Extension.
    /// Might be another Extension or the root.
    /// </summary>
    public ADataExtendable<T> Parent => _parent;
    
    /// <summary>
    /// Gets the base datatype instance.
    /// </summary>
    public ADataExtendable<T> Root => _root;
    
    
    /// <summary>
    /// Gets the set of extensions of type <typeparamref name="T"/>.
    /// </summary>
    public ISet<T> Extensions => _extensions;
    
    #endregion
    
    
    #region DataExtension - Constructors

    /// <summary>
    /// Constructor to initialize a <see cref="ADataExtension{T}"/> instance with a specified extension ID.
    /// </summary>
    /// <param name="extensionId">The unique ID for the data extension.</param>
    /// <param name="parent">The parent Extendable of this Extension.</param>
    /// <param name="root">The base datatype instance.</param>
    protected ADataExtension(string extensionId, ADataExtendable<T> parent, ADataExtendable<T> root)
    {
        _extensionId = extensionId;
        
        _parent = parent;
        _root = root;
        
        _extensions = new HashSet<T>();
        
        _rwls = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Constructor to initialize a <see cref="ADataExtension{T}"/> instance with a specified extension ID and a set of extensions.
    /// </summary>
    /// <param name="extensionId">The unique ID for the data extension.</param>
    /// <param name="parent">The parent Extendable of this Extension.</param>
    /// <param name="root">The base datatype instance.</param>
    /// <param name="extensions">A pre-defined set of extensions of type <typeparamref name="T"/>.</param>
    /// 
    protected ADataExtension(string extensionId, ADataExtendable<T> parent, ADataExtendable<T> root, ISet<T> extensions)
    {
        _extensionId = extensionId;
        
        _parent = parent;
        _root = root;
        
        _extensions = extensions;
        
        _rwls = new ReaderWriterLockSlim();
    }
        
    #endregion
    
    
    #region DataExtension - Methods

    
    public virtual bool AddDataExtension(IDataExtension dataExtension)
    {
        if (dataExtension == null)
        {
            throw new ArgumentExceptionWithReturnCode($"{nameof(dataExtension)} is null.", 1);
        }
        
        if (dataExtension is not T extension)
        {
            throw new InvalidOperationExceptionWithReturnCode($"Extension of type {dataExtension.GetType().Name} is not supported.", 2);
        }

        if (_extensions == null)
        {
            throw new InvalidOperationExceptionWithReturnCode($"{nameof(_extensions)} is null.", 3);
        }
        

        bool rValue = false;
        
        
        _rwls.EnterWriteLock();
        
        rValue = _extensions.Add(extension);
        
        _rwls.ExitWriteLock();
        

        return rValue;
    }

    
    public virtual bool RemoveDataExtension(string extensionId)
    {
        if (_extensions == null)
        {
            throw new InvalidOperationExceptionWithReturnCode($"{nameof(_extensions)} is null.", 1);
        }
        
        if (_extensions.IsReadOnly)
        {
            throw new InvalidOperationExceptionWithReturnCode($"{nameof(extensionId)} is read-only!", 2);
        }
        
        if (extensionId == null)
        {
            throw new ArgumentNullExceptionWithReturnCode($"{nameof(extensionId)} is null!", 3);
        }

        if (extensionId.Length == 0)
        {
            throw new ArgumentExceptionWithReturnCode($"{nameof(extensionId)} is empty!", 4);
        }
        
        
        if (_extensions.Count == 0)
        {
            return false;
        }
        
        
        try
        {
            bool rValue = false;
            
            
            _rwls.EnterReadLock();
            
            T dataExtension = _extensions.First(x =>
                string.Equals(x.GetExtensionId(), extensionId, StringComparison.CurrentCultureIgnoreCase));
            
            _rwls.ExitReadLock();
            

            _rwls.EnterWriteLock();
            
            rValue = _extensions.Remove(dataExtension);
            
            _rwls.ExitWriteLock();
            
            
            return rValue;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Exception e)
        {
            throw new ExceptionWithReturnCode($"Unexpected Exception:\n{e}", int.MaxValue);
        }
    }

    
    public virtual IDataExtension GetDataExtension(string extensionId)
    {
        if (extensionId == null)
        {
            throw new ArgumentNullExceptionWithReturnCode($"{nameof(extensionId)} is null!", 1);
        }

        if (extensionId.Length == 0)
        {
            throw new ArgumentExceptionWithReturnCode($"{nameof(extensionId)} is empty!", 2);
        }
        
        if (_extensions == null)
        {
            throw new InvalidOperationExceptionWithReturnCode($"{nameof(_extensions)} is null.", 3);
        }
        
        if (_extensions.Count == 0)
        {
            throw new ArgumentExceptionWithReturnCode($@"{nameof(_extensions)} is empty.", 4);
        }

        
        try
        {
            T rValue;
            
            
            _rwls.EnterWriteLock();
            
            rValue = _extensions.First(x => string.Equals(x.GetExtensionId(), extensionId, StringComparison.CurrentCultureIgnoreCase));
            
            _rwls.ExitWriteLock();
            
            
            return rValue;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationExceptionWithReturnCode($@"Was not able to find a {nameof(T)} with an id equal to {extensionId}!", 5);
        }
        catch (Exception e)
        {
            throw new ExceptionWithReturnCode($"Unexpected Exception:\n{e}", int.MaxValue);
        }
    }

    /// <summary>
    /// Gets the unique identifier of this data extension.
    /// </summary>
    /// <returns>The unique ID of this data extension.</returns>
    public virtual string GetExtensionId()
    {
        return _extensionId;
    }

    #endregion
    
    
    #region Equality Members

    /// <summary>
    /// Determines whether the current data extension is equal to another <see cref="ADataExtension{T}"/> instance.
    /// </summary>
    /// <param name="other">The other <see cref="ADataExtension{T}"/> instance to compare with.</param>
    /// <returns><c>true</c> if the current instance is equal to the specified instance; otherwise, <c>false</c>.</returns>
    protected bool Equals(ADataExtension<T> other)
    {
        return _extensionId == other._extensionId && _parent.Equals(other._parent) && _root.Equals(other._root);
    }

    /// <summary>
    /// Determines whether the current data extension is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the current instance is equal to the specified object; otherwise, <c>false</c>.</returns>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ADataExtension<T>)obj);
    }
    
    /// <summary>
    /// Returns the hash code for this data extension, based on the extension ID.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return _extensionId.GetHashCode();
    }

    #endregion
}