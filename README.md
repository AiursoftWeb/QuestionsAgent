# Aiursoft QuestionsAgent

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/QuestionsAgent/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/aiursoft/QuestionsAgent/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/QuestionsAgent/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/aiursoft/QuestionsAgent/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/QuestionsAgent/-/pipelines)
[![NuGet version (Aiursoft.QuestionsAgent)](https://img.shields.io/nuget/v/Aiursoft.QuestionsAgent.svg)](https://www.nuget.org/packages/Aiursoft.QuestionsAgent/)
[![Man hours](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/questionsagent.svg)](https://manhours.aiursoft.com/r/gitlab.aiursoft.com/aiursoft/questionsagent.html)


A CLI tool to parse markdown question files into JSON format using AI.

## Install

Requirements:

1. [.NET 10 SDK](http://dot.net/)

Run the following command to install this tool:

```bash
dotnet tool install --global Aiursoft.QuestionsAgent
```

## Usage

After getting the binary, run it directly in the terminal.

```bash
$ questions-agent process --input processing.md --instance http://localhost:11434/api/chat --model qwen --token your-token
```

### Options

* `--input (-i)`: The input markdown file to process. (Required)
* `--output (-o)`: The output directory for JSON files. (Default: `FinalOutput`)
* `--instance`: The Ollama instance to use. (Required)
* `--model`: The Ollama model to use. (Required)
* `--token`: The Ollama token to use. (Required)

## Helper Tools

### Word to Markdown Converter

If you have Word documents (.doc, .docx) that need to be converted to Markdown first, use the helper script in [`assets/`](assets/):

```bash
# Convert Word documents to Markdown
python3 assets/word_to_markdown.py ~/Documents/Exams all_exams.md

# Then process with questions-agent
questions-agent process --input all_exams.md --output results \
  --instance http://localhost:11434/api/chat \
  --model qwen --token your-token
```

See [`assets/README.md`](assets/README.md) for details.