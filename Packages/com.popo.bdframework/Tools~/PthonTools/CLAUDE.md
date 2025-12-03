# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Python utility library for uploading files to FileBrowser, a web-based file management system. The project follows a simple, utility-focused architecture where each Python script serves a specific purpose and can be called externally with command-line arguments.

## Development Commands

### Testing
```bash
# Run all tests
pytest tests/

# Run a specific test file
pytest tests/upload_to_dufs_test.py

# Run tests with verbose output
pytest -v tests/
```

### Virtual Environment
```bash
# The project uses a virtual environment located at .venv/
# Activate it (Windows)
source .venv/Scripts/activate

# Activate it (Unix/MacOS)
source .venv/bin/activate
```

### Package Management
```bash
# Install dependencies (using poetry)
poetry install

# Install only runtime dependencies
poetry install --no-dev
```

## Architecture

### Core Components

1. **FileBrowserClient** (`src/upload_to_filebrowser.py`):
   - Handles authentication (Bearer token or username/password)
   - Manages directory creation on the FileBrowser server
   - Performs file uploads with proper error handling

2. **File Iteration System** (`src/upload_to_filebrowser.py`):
   - Supports both single files and directory uploads
   - Maintains directory structure when uploading folders
   - Uses pathlib for cross-platform file operations

3. **CLI Interface** (`src/upload_to_filebrowser.py`):
   - Command-line argument parsing with argparse
   - Support for TLS certificate verification control
   - Flexible authentication options

### Design Philosophy

Based on the Chinese comment in Readme.txt: "This is a Python utility library, all Python designs are meant to be called externally like 'py xxxx.py' arguments. So I hope each functionality is in one Python file, with unit tests, implementation should be simple, don't do too much abstraction."

The architecture follows these principles:
- **Single Responsibility**: Each Python file handles one specific functionality
- **Simplicity**: Avoid excessive abstraction and complexity
- **External Usability**: All tools are designed to be called from external scripts
- **Testability**: Each functionality has corresponding unit tests

### Dependencies

- **requests**: For HTTP communication with FileBrowser API
- **pytest**: For unit testing (development dependency)

### Key Functionality

The main entry point (`run_upload` and `main` functions) provides:
1. File/directory existence validation
2. FileBrowser API connection establishment
3. Recursive directory structure creation
4. File-by-file upload with progress reporting
5. Comprehensive error handling and status reporting

### Testing Strategy

Tests use a mock HTTP server approach to validate:
- Directory creation API calls
- File upload functionality
- Proper relative path handling
- Error conditions and edge cases