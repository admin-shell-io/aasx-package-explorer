/*
 * Copyright (c) 2020 SICK AG <info@sick.de>
 *
 * This software is licensed under the Apache License 2.0 (Apache-2.0).
 * The ExcelDataReder dependency is licensed under the MIT license
 * (https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/LICENSE).
 */

#nullable enable

using System;
using System.IO;

namespace AasxDictionaryImport.Tests
{
    /// <summary>
    /// A wrapper around a path string that will recursively delete the directory at the wrapped path when it is
    /// disposed (if the directory exists).
    /// </summary>
    internal sealed class TempDir : IDisposable
    {
        public string Path { get; }

        private TempDir(string path)
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }

        /// <summary>
        /// Generate and create a new temporary directory.
        /// </summary>
        /// <returns>A new temporary directory that has been created on the file system</returns>
        public static TempDir Create() => Create(System.IO.Path.GetTempPath());

        /// <summary>
        /// Generate and create a new temporary directory within the given base directory.
        /// </summary>
        /// <param name="baseDir">The base directory for the created directory</param>
        /// <returns>A new temporary directory that has been created on the file system</returns>
        public static TempDir Create(string baseDir)
        {
            var tempDir = Generate(baseDir);
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        /// <summary>
        /// Generate a temporary directory but do not create it.
        /// </summary>
        /// <returns>A new temporary directory that has not yet been created on the file system</returns>
        public static TempDir Generate() => Generate(System.IO.Path.GetTempPath());

        /// <summary>
        /// Generate a temporary directory within the given base directory but do not create it.
        /// </summary>
        /// <param name="baseDir">The base directory for the generated directory</param>
        /// <returns>A new temporary directory that has not yet been created on the file system</returns>
        public static TempDir Generate(string baseDir)
        {
            return new TempDir(System.IO.Path.Combine(baseDir, System.IO.Path.GetRandomFileName()));
        }

        public static implicit operator string(TempDir tempDir) => tempDir.Path;
    }
}
