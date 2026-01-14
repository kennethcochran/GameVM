-- MAME monitor script for GameVM validation
local frames = 0
local done = false

emu.register_frame(function()
    if done then return end
    frames = frames + 1
    
    -- Wait for about half a second (30 frames at 60fps)
    if frames >= 30 then
        done = true
        local cpu = manager.machine.devices[":maincpu"]
        local mem = cpu.spaces["program"]
        
        print("--- GAMEVM MAME DUMP ---")
        print("CPU state:")
        print(string.format("A: %d", cpu.state["A"].value))
        print(string.format("PC: %04X", cpu.state["PC"].value))
        print(string.format("X: %d", cpu.state["X"].value))
        print(string.format("Y: %d", cpu.state["Y"].value))
        
        print("Memory at $F000:")
        for i=0,7 do
            print(string.format("%04X: %02X", 0xF000 + i, mem:read_u8(0xF000 + i)))
        end
        
        print("TIA/RAM Dump:")
        print("Done.")
        
        manager.machine:exit()
    end
end)
