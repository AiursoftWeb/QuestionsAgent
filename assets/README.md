# Assets 目录

这个目录包含 Aiursoft.QuestionsAgent 项目的辅助工具脚本。

## word_to_markdown.py

将 Word 文档(.doc, .docx)转换为单个 Markdown 文件的独立Python脚本。

### 依赖

```bash
# 安装 Python 包
pip3 install --break-system-packages markitdown

# 可选：安装 LibreOffice 以支持 .doc 文件
sudo apt install libreoffice
```

### 使用

```bash
python3 word_to_markdown.py <源目录> <输出文件>
```

### 示例

```bash
# 转换 exams 目录下的所有 Word 文档
python3 assets/word_to_markdown.py ~/Documents/Exams all_exams.md

# 然后用 questions-agent 处理转换后的 Markdown
questions-agent process --input all_exams.md --output results \
  --instance http://localhost:11434/api/chat \
  --model qwen \
  --token your-token
```

### 完整工作流程

```bash
# 步骤 1: 转换 Word → Markdown
cd /path/to/Aiursoft.QuestionsAgent
python3 assets/word_to_markdown.py ~/exam_files exam_archive.md

# 步骤 2: 提取题目 → JSON
questions-agent process \
  --input exam_archive.md \
  --output question_database \
  --instance https://ollama.aiursoft.com/api/chat \
  --model qwen3:30b \
  --token $OLLAMA_TOKEN

# 步骤 3: 获取结果
ls question_database/
# 选择.json  填空.json  判断.json  名词解释.json  简答.json
```

### 功能特性

- ✅ 递归扫描目录
- ✅ 支持 .docx 文件
- ✅ 支持 .doc 文件 (需要 LibreOffice)
- ✅ 自动跳过非文档文件
- ✅ 保留源文件路径信息
- ✅ 合并为单个 Markdown 文件

### 故障排除

#### markitdown 安装失败

Python 3.13+ 需要使用 `--break-system-packages`:

```bash
pip3 install --break-system-packages markitdown
```

或使用 pipx:

```bash
sudo apt install pipx
pipx install markitdown
```

#### LibreOffice 未找到

如果不需要转换 .doc 文件,可以忽略此警告。如果需要:

```bash
sudo apt install libreoffice
```

验证安装:

```bash
soffice --version
```
