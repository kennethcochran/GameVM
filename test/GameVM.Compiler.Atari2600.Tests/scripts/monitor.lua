-- GameVM MAME Monitor Script
-- Dumps CPU state to stdout for verification in tests

local function dump_state()
    local cpu = manager.machine.devices[":maincpu"]
    
    print("--- GAMEVM MAME DUMP ---")
    -- Use pc(), state() methods or debug interface
    -- In MAME Lua, cpu.state[name] should work if items is nil, 
    -- but sometimes it's cpu:state():set_value(name, val) or cpu:state():value(name)
    
    -- Let's try to just dump all registers using a more robust way
    print("CPU state:")
    -- Use specific register names if pairs fails or returns unexpected objects
    print("A: " .. string.format("%02X", cpu.state["A"].value))
    print("X: " .. string.format("%02X", cpu.state["X"].value))
    print("Y: " .. string.format("%02X", cpu.state["Y"].value))
    print("PC: " .. string.format("%04X", cpu.state["PC"].value))

    print("TIA/RAM Dump:")
    -- Dump some zero page
    -- Use cpu.spaces["program"] and its read methods
    local mem = cpu.spaces["program"]
    for i = 0x80, 0x85 do
        -- MAME Lua 0.2xx+ uses read_u8 or similar
        local val = 0
        pcall(function() val = mem:read_u8(i) end)
        if val == 0 then pcall(function() val = mem:read_byte(i) end) end
        
        print(string.format("$%02X: %02X", i, val))
    end
    print("--- END GAMEVM DUMP ---")
end

-- Wait for some frames to allow the program to run
local frames = 0
local function on_frame()
    frames = frames + 1
    if frames == 30 then
        dump_state()
        manager.machine:exit()
    end
end

emu.register_frame(on_frame)
