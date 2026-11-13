# GitHub Copilot Instructions for CaveworldFlora (Continued) RimWorld Mod

## Mod Overview and Purpose
CaveworldFlora (Continued) is a RimWorld mod that adds various flora to the cave biome. It works well with the CaveBiome (continued) mod. The mod introduces new plants, such as the Black Lotus, Gleamcap, and DevilTongue, which can be found and used within the cave environment.

## Key Features and Systems
- **Cluster Plants**: The mod includes several types of cluster plants that can grow and interact with their environment. Each plant has unique behaviors and growth patterns.
- **Reproduction System**: The mod features a system that governs the reproduction and spread of these plants within the cave.
- **XML Integration**: Various XML files define the properties and behavior of the new flora and related elements, such as effects and research projects.
- **Harmony Patching**: The mod uses Harmony patching to integrate with existing RimWorld systems and modify their behavior as needed.

## Coding Patterns and Conventions
- **C# Files**: The mod's C# files are located in the `Source` directory. Each file corresponds to a specific type of plant or system within the mod.
- **Class Structure**: Plant types are defined as classes, with methods and properties that govern their behavior, such as growth, reproduction, and interaction with the environment.
- **XML Files**: XML files are located in the `Defs` directory. They define various aspects of the mod, such as new plants, effects, and research projects.
- **Harmony Patching**: Harmony patches are used to modify existing RimWorld code. They are typically located in separate files within the `Source` directory.

## XML Integration
CaveworldFlora (Continued) heavily relies on XML files to define new elements and modify existing ones. Key XML files include:
- **ThingDefs**: Define new plants and other in-game objects.
- **HediffDefs**: Define new health conditions and effects that can be applied to pawns.
- **Effects**: Define visual effects and other in-game phenomena.
- **ResearchProjectDefs**: Define new research projects that can be conducted in the game.

## Harmony Patching
Harmony patching is used to modify existing RimWorld code to integrate new functionality. Common areas of modification include:
- **Plant Growth**: Modifying plant growth rates and conditions.
- **Interaction**: Modifying how plants interact with the environment and other objects.
- **Behavior**: Modifying the behavior of existing RimWorld systems to accommodate new flora.

## Suggestions for Copilot
To make the most of GitHub Copilot when working on CaveworldFlora (Continued), consider the following suggestions:
- **Explore C# Files**: Use Copilot to explore and understand the C# files that define the behavior of the new plants.
- **Generate XML Snippets**: Use Copilot to generate XML snippets for new plants, effects, and research projects.
- **Harmony Patch Templates**: Use Copilot to generate templates for Harmony patches to modify existing RimWorld code.
- **Debugging Assistance**: Use Copilot to assist with debugging by generating code snippets that can help identify and fix issues.
- **Documentation Generation**: Use Copilot to generate documentation for new classes and methods to help with understanding and maintaining the mod.

By following these guidelines, you can effectively use GitHub Copilot to enhance your modding experience for CaveworldFlora (Continued).

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

