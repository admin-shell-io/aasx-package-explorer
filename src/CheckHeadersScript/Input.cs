using ArgumentException = System.ArgumentException;
using StringComparer = System.StringComparer;
using Path = System.IO.Path;

using System.Collections.Generic;
using System.Linq;

namespace CheckHeadersScript
{
    public static class Input
    {
                /// <summary>
        /// Matches all the files defined by the patterns, includes and excludes.
        /// If any of the patterns is given as a relative directory,
        /// current working directory is prepended.
        /// </summary>
        /// <param name="cwd">current working directory</param>
        /// <param name="patterns">GLOB patterns to match files for inspection</param>
        /// <param name="excludes">GLOB patterns to exclude files matching patterns</param>
        /// <returns>Paths of the matched files</returns>
        public static IEnumerable<string> MatchFiles(
            string cwd,
            List<string> patterns,
            List<string> excludes)
        {
            ////
            // Pre-condition(s)
            ////

            if (cwd.Length == 0)
            {
                throw new ArgumentException("Expected a non-empty cwd");
            }

            if (!Path.IsPathRooted(cwd))
            {
                throw new ArgumentException("Expected cwd to be rooted");
            }

            ////
            // Implementation
            ////

            if (patterns.Count == 0)
            {
                yield break;
            }

            var globExcludes = excludes.Select(
                (pattern) =>
                {
                    string rootedPattern = (Path.IsPathRooted(pattern))
                        ? pattern
                        : Path.Join(cwd, pattern);

                    return new GlobExpressions.Glob(rootedPattern);
                }).ToList();

            foreach (var pattern in patterns)
            {
                IEnumerable<string>? files;

                if (Path.IsPathRooted(pattern))
                {
                    var root = Path.GetPathRoot(pattern);
                    if (root == null)
                    {
                        throw new ArgumentException(
                            $"Root could not be retrieved from rooted pattern: {pattern}");
                    }

                    var relPattern = Path.GetRelativePath(root, pattern);

                    files = GlobExpressions.Glob.Files(root, relPattern)
                        .Select((path) => Path.Join(root, path));
                }
                else
                {
                    files = GlobExpressions.Glob.Files(cwd, pattern);
                }

                List<string> accepted =
                    files
                        .Where((path) =>
                        {
                            string rootedPath = (Path.IsPathRooted(path))
                                ? path
                                : Path.Join(cwd, path);

                            return globExcludes.TrueForAll((glob) => !glob.IsMatch(rootedPath));
                        })
                        .ToList();

                accepted.Sort(StringComparer.InvariantCulture);

                foreach (string path in accepted)
                {
                    yield return path;
                }
            }
        }
    }
}
