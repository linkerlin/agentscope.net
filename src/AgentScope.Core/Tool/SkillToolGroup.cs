// Copyright 2024-2026 the original author or authors.
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

using System;
using System.Collections.Generic;

namespace AgentScope.Core.Tool
{
    public class SkillToolGroup
    {
        public string Name { get; }
        public bool IsActive { get; set; }
        public List<ITool> Tools { get; }

        public SkillToolGroup(string name, IEnumerable<ITool> tools, bool isActive = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Tools = tools?.ToList() ?? new List<ITool>();
            IsActive = isActive;
        }
    }
}
