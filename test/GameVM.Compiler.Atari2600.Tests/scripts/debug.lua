print("--- EMU TABLE ---")
if emu then
    for k, v in pairs(emu) do
        print(k, type(v))
    end
else
    print("emu is nil")
end
print("--- END EMU TABLE ---")
manager.machine:exit()
