cmake_minimum_required(VERSION 3.20)

project(GitHubCopilotAgentBot LANGUAGES CSharp)

# Enable CSharp language support
enable_language(CSharp)

# Set C# language version
set(CMAKE_CSharp_FLAGS "/langversion:latest")

# Define the executable
add_executable(GitHubCopilotAgentBot
    src/Program.cs
    src/GitHubService.cs
    src/NotificationService.cs
    src/NotificationHistory.cs
    src/Configuration.cs
    src/Models/PullRequestReview.cs
    src/Models/NotificationEntry.cs
)

# Set project properties
set_target_properties(GitHubCopilotAgentBot PROPERTIES
    VS_DOTNET_TARGET_FRAMEWORK_VERSION "v6.0"
    VS_DOTNET_REFERENCES "System;System.Net.Http;System.Text.Json"
)

# Set output directory
set_target_properties(GitHubCopilotAgentBot PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)

# Add NuGet package references
set_property(TARGET GitHubCopilotAgentBot PROPERTY
    VS_PACKAGE_REFERENCES "Octokit_11.0.1;System.Text.Json_8.0.5"
)
