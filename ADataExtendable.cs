﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MyDevTime.DataExtendable.Interfaces;
using MyDevTime.ExceptionsWithReturnCode;

namespace MyDevTime.DataExtendable.Abstracts;

/// <summary>
/// A class that manages a collection of data extensions of type <typeparamref name="T"/>.
/// Provides methods to add, remove, and retrieve extensions based on their unique extension ID.
/// </summary>
/// <typeparam name="T">The type of data extension, which must implement <see cref="IDataExtension"/>.</typeparam>
public class ADataExtendable<T>
    where T : IDataExtension
{
    #region DataExtendable - Fields
    
    /// <summary>
    /// A set of extensions of type <typeparamref name="T"/>.
    /// </summary>
    private ISet<T> _extensions;
    
    /// <summary>
    /// A lock to manage read and write access in this class
    /// </summary>
    private ReaderWriterLockSlim _rwls;
    #endregion
    
    
    #region DataExtendable - Properties

    /// <summary>
    /// Gets the set of extensions of type <typeparamref name="T"/>.
    /// </summary>
    public ISet<T> Extensions => _extensions;

    #endregion
    
    
    #region DataExtendable - Constructors
    
    /// <summary>
    /// Parameterless constructor to initialize a <see cref="ADataExtendable{T}"/>. <br/>
    /// It initializes <see cref="_extensions"/>.
    /// </summary>
    public ADataExtendable()
    {
        _extensions = new HashSet<T>();
        _rwls = new ReaderWriterLockSlim();
    }
    
    /// <summary>
    /// Constructor to initialize a <see cref="ADataExtendable{T}"/> instance with a set of extensions.
    /// </summary>
    /// <param name="extensions">A pre-defined set of extensions of type <typeparamref name="T"/>.</param>
    public ADataExtendable(ISet<T> extensions)
    {
        _extensions = extensions;
        _rwls = new ReaderWriterLockSlim();
    }

    #endregion
    
    
    #region DataExtendable - Methods

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

    #endregion    
}