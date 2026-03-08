// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Incursa.Platform.Storage;

/// <summary>
/// Represents a storage-specific failure surfaced by a storage provider.
/// </summary>
public class StorageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageException"/> class.
    /// </summary>
    public StorageException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public StorageException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents an optimistic-concurrency or mutation precondition failure.
/// </summary>
public sealed class StoragePreconditionFailedException : StorageException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePreconditionFailedException"/> class.
    /// </summary>
    public StoragePreconditionFailedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePreconditionFailedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public StoragePreconditionFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePreconditionFailedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StoragePreconditionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents an intentionally unsupported storage operation.
/// </summary>
public sealed class StorageOperationNotSupportedException : StorageException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageOperationNotSupportedException"/> class.
    /// </summary>
    public StorageOperationNotSupportedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageOperationNotSupportedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public StorageOperationNotSupportedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageOperationNotSupportedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public StorageOperationNotSupportedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
