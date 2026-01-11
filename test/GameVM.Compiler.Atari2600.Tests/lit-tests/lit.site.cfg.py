import os
import sys
import platform
import lit.formats

# Configuration for lit testing.
config.name = 'GameVM Compiler Tests'
config.suffixes = ['.pas', '.txt']

# Set the root directories
config.test_source_root = os.path.dirname(os.path.abspath(__file__))
config.test_exec_root = os.path.join(config.test_source_root, 'test_output')

# Use ShTest to execute the RUN: commands
config.test_format = lit.formats.ShTest(execute_external=True)

# Ensure the output directory exists
if not os.path.exists(config.test_exec_root):
    os.makedirs(config.test_exec_root)

# Setup substitutions
script_dir = os.path.abspath(os.path.join(config.test_source_root, '..', 'scripts')).replace('\\', '/')
filecheck_py = os.path.join(script_dir, 'filecheck.py').replace('\\', '/')
bincheck_sh = os.path.join(script_dir, 'bincheck.sh').replace('\\', '/')

# Use dotnet dll execution for stability across Linux environments
project_root = os.path.abspath(os.path.join(config.test_source_root, '..', '..', '..')).replace('\\', '/')
compile_project_bin = os.path.join(project_root, 'src', 'GameVM.Compile', 'bin', 'Debug', 'net8.0').replace('\\', '/')
compiler_dll = os.path.join(compile_project_bin, 'GameVM.Compile.dll').replace('\\', '/')

# Verification that dll exists
if not os.path.exists(compiler_dll):
    # Try release if debug is missing
    release_bin = os.path.join(project_root, 'src', 'GameVM.Compile', 'bin', 'Release', 'net8.0')
    if os.path.exists(os.path.join(release_bin, 'GameVM.Compile.dll')):
        compiler_dll = os.path.join(release_bin, 'GameVM.Compile.dll')

compiler_cmd = f"dotnet {compiler_dll}"

config.substitutions.append(('%compile', compiler_cmd))
config.substitutions.append(('%FileCheck', f"python3 {filecheck_py}"))
config.substitutions.append(('%bincheck', f"bash {bincheck_sh}"))
# MAME Execution Verification
dev_tool_dll = os.path.join(project_root, 'src', 'GameVM.DevTools', 'bin', 'Debug', 'net8.0', 'GameVM.DevTools.dll').replace('\\', '/')
dev_tool = f"dotnet {dev_tool_dll}"
monitor_lua = os.path.join(script_dir, 'monitor.lua').replace('\\', '/')

config.substitutions.append(('%mame-run', f"{dev_tool} mame run --script {monitor_lua} --rom"))

# Exclude specific directories
config.excludes = ['test_output']
