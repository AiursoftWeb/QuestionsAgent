#!/usr/bin/env python3
"""
Word to Markdown Converter

Converts all Word documents (.doc, .docx) in a directory to a single Markdown file.
This is a standalone utility script for the Aiursoft.QuestionsAgent project.

Usage:
    python3 word_to_markdown.py <source_directory> <output_file>

Example:
    python3 word_to_markdown.py ~/Documents/Exams all_exams.md

Dependencies:
    pip3 install --break-system-packages markitdown
    sudo apt install libreoffice  # Optional, for .doc support
"""
import os
import sys
import subprocess
import tempfile
import shutil
from markitdown import MarkItDown

def convert_doc_to_docx(doc_path):
    """
    Convert .doc to .docx using LibreOffice in a temporary directory.
    Returns the path to the converted .docx file and the temp directory path.
    """
    temp_dir = tempfile.mkdtemp()
    try:
        if shutil.which("soffice") is None:
            print("    [Warning] LibreOffice not found. Skipping .doc conversion.", file=sys.stderr)
            return None, temp_dir

        subprocess.run([
            "soffice", "--headless", "--convert-to", "docx", 
            "--outdir", temp_dir, doc_path
        ], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        
        filename = os.path.basename(doc_path)
        docx_name = os.path.splitext(filename)[0] + ".docx"
        converted_path = os.path.join(temp_dir, docx_name)
        
        return (converted_path, temp_dir) if os.path.exists(converted_path) else (None, temp_dir)
    except Exception as e:
        print(f"    [Error] Failed to convert .doc: {doc_path} -> {e}", file=sys.stderr)
        return None, temp_dir

def main():
    if len(sys.argv) < 3:
        print("Usage: word_to_markdown.py <source_directory> <output_file>", file=sys.stderr)
        print("\nExample:", file=sys.stderr)
        print("  python3 word_to_markdown.py ~/Documents/Exams all_exams.md", file=sys.stderr)
        sys.exit(1)
    
    source_dir = sys.argv[1]
    output_file = sys.argv[2]
    
    ignore_dirs = [".git", "__pycache__", ".ipynb_checkpoints", ".gemini", ".agent", 
                   "bin", "obj", "node_modules"]
    
    if not os.path.isdir(source_dir):
        print(f"Error: Source directory '{source_dir}' does not exist.", file=sys.stderr)
        sys.exit(1)
    
    md = MarkItDown()
    total_files = 0
    abs_output_path = os.path.abspath(output_file)
    
    print(f"Starting Word to Markdown conversion...")
    print(f"Source: {os.path.abspath(source_dir)}")
    print(f"Output: {abs_output_path}")

    with open(output_file, "w", encoding="utf-8") as out_f:
        out_f.write(f"# 文档归档\n\n自动生成的 Markdown 文档集合\n\n")

        for root, dirs, files in os.walk(source_dir):
            dirs[:] = [d for d in dirs if d not in ignore_dirs]

            for file in files:
                file_path = os.path.join(root, file)
                
                if os.path.abspath(file_path) == abs_output_path:
                    continue

                ext = os.path.splitext(file)[1].lower()
                if ext not in ['.doc', '.docx']:
                    continue

                temp_dir_to_clean = None
                target_path = file_path
                should_process = False

                print(f"Processing: {file} ...")

                try:
                    if ext == '.doc':
                        converted_path, temp_dir = convert_doc_to_docx(file_path)
                        if converted_path:
                            target_path = converted_path
                            temp_dir_to_clean = temp_dir
                            should_process = True
                        else:
                            print(f"    [Skip] Could not convert .doc file.")
                            if temp_dir:
                                shutil.rmtree(temp_dir)
                            continue
                    elif ext == '.docx':
                        should_process = True

                    if should_process:
                        try:
                            result = md.convert(target_path)
                            if result and result.text_content:
                                relative_path = os.path.relpath(file_path, source_dir)
                                header = f"\n\n---\n\n# 来源文件：{file}\n## 路径：{relative_path}\n\n"
                                out_f.write(header)
                                out_f.write(result.text_content)
                                total_files += 1
                                print(f"    [OK] Added to archive.")
                            else:
                                print(f"    [Warning] No content extracted.")
                        except Exception as e:
                            print(f"    [Error] MarkItDown extraction failed: {e}", file=sys.stderr)

                except Exception as e:
                    print(f"    [Error] Unexpected error {file}: {e}", file=sys.stderr)
                
                if temp_dir_to_clean and os.path.exists(temp_dir_to_clean):
                    shutil.rmtree(temp_dir_to_clean)

    print(f"\n========================================")
    print(f"Processing complete! Converted {total_files} files.")
    print(f"Output file: {abs_output_path}")
    
    return 0

if __name__ == "__main__":
    sys.exit(main())
