﻿using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Sdk.Health
{
    /// <summary>
    /// Health service.
    /// </summary>
    [PublicAPI]
    public class HealthService : IHealthService
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _issuesDict = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        /// <inheritdoc/>
        public void ClearAllIssues()
        {
            _issuesDict.Clear();
        }

        /// <inheritdoc/>
        public void ClearIssuesByType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException();
            _issuesDict.TryRemove(type, out var _);
        }

        /// <inheritdoc/>
        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var result = new List<HealthIssue>();
            foreach (var key in _issuesDict.Keys)
            {
                var typeIssues = _issuesDict[key];
                while (typeIssues.TryTake(out string value))
                    result.Add(HealthIssue.Create(key, value));
            }

            return result;
        }

        /// <inheritdoc/>
        public string GetHealthViolationMessage()
        {
            return string.Join(", ", GetHealthIssues().Select(i => $"{i.Type} : {i.Value}"));
        }

        /// <inheritdoc/>
        public void Register(string type, string value)
        {
            if (!_issuesDict.ContainsKey(type))
                _issuesDict.TryAdd(type, new ConcurrentBag<string>());
            _issuesDict[type].Add(value);
        }
    }
}
