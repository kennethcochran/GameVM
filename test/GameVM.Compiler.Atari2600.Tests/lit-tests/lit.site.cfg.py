import os

config.name = 'GameVM Compiler Tests'
config.suffixes = ['.pas', '.txt']
config.test_source_root = os.path.dirname(__file__)
config.test_exec_root = os.path.join(config.test_exec_root, 'test_output')

# Test configuration
config.substitutions.append(('%compile', r'/home/kenneth/Projects/GameVM/src/GameVM.Compile/bin/Debug/net8.0/compile.exe'))
config.substitutions.append(('%FileCheck', 'FileCheck'))
