import os
import platform
import lit.llvm
from lit.llvm import llvm_config

# Configuration for lit testing.
config.name = 'GameVM Compiler Tests'
config.test_format = lit.formats.ShTest(not llvm_config.use_lit_shell)

# Test suffixes (file extensions) to recognize
extensions = [
    '.pas',    # Pascal source files
    '.txt',    # Test specification files
    '.ll',     # LLVM IR files (if applicable)
    '.s',      # Assembly files (if applicable)
    '.test'    # Generic test files
]
config.suffixes = [s for s in extensions if s]

# Exclude specific directories and files
config.excludes = [
    'Inputs',     # Input directory for tests
    'Output',     # Output directory for tests
    'CMakeLists.txt',
    'README.txt',
    'LICENSE.txt'
]

# Setup source and execution roots
config.test_source_root = os.path.dirname(os.path.abspath(__file__))
config.test_exec_root = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Output')

# Ensure the output directory exists
os.makedirs(config.test_exec_root, exist_ok=True)

# Setup the environment
config.environment = {}

# Determine the target triple if needed
target_triple = ''
if platform.system() == 'Windows':
    target_triple = f"{platform.machine().lower()}-pc-windows-msvc"
elif platform.system() == 'Linux':
    target_triple = f"{platform.machine().lower()}-unknown-linux-gnu"
elif platform.system() == 'Darwin':
    target_triple = f"{platform.machine().lower()}-apple-darwin"

# Setup substitutions
compile_cmd = ''  # Will be set by the PowerShell script
config.substitutions = [
    # Tool substitutions
    ('%{compile}', compile_cmd),
    ('%{FileCheck}', 'FileCheck'),
    
    # Path substitutions
    ('%{test_exec_root}', config.test_exec_root),
    ('%{test_source_root}', config.test_source_root),
    
    # Platform-specific substitutions
    ('%{target_triple}', target_triple),
    ('%{host_cc}', 'gcc' if platform.system() == 'Linux' else 'clang' if platform.system() == 'Darwin' else 'cl.exe'),
]

# Features available for test conditions
config.available_features = ['host-run-tool']

# Add host-specific features
if platform.system() == 'Windows':
    config.available_features.append('system-windows')
elif platform.system() == 'Linux':
    config.available_features.append('system-linux')
elif platform.system() == 'Darwin':
    config.available_features.append('system-darwin')

# Add architecture-specific features
if platform.machine() in ('x86_64', 'AMD64'):
    config.available_features.append('x86_64')
elif platform.machine() in ('aarch64', 'arm64'):
    config.available_features.append('aarch64')

# LLVM-specific configuration
llvm_config.with_environment('PATH', [], append_path=True)
llvm_config.with_environment('LD_LIBRARY_PATH', [], append_path=True)
