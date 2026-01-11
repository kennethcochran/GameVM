-- GameVM MAME Monitor Script
-- Dumps hardware state and exits

local cpu = manager.machine.devices[":maincpu"]
local mem = cpu.spaces["program"]

-- Configuration
local frames = 0
local TARGET_FRAMES = 120 -- Run for 2 seconds

function dump_state()
    print("--- GAMEVM MAME DUMP ---")
    
    print("CPU state:")
    for k, v in pairs(cpu.state) do
       print(k .. ": " .. v.value)
    end

    -- Dump TIA and RAM (0x00 to 0xFF)
    print("TIA/RAM Dump:")
    for i = 0, 255 do
        local val = mem:read_u8(i)
        if i % 16 == 0 then
            io.write(string.format("\n%04X: ", i))
        end
        io.write(string.format("%02X ", val))
    end
    
    -- Dump ROM Start (0xF000)
    print("\nROM Dump ($F000):")
    for i = 0xF000, 0xF00F do
        local val = mem:read_u8(i)
        io.write(string.format("%02X ", val))
    end
    
    -- Dump Vectors ($FFFA)
    print("\nVectors Dump ($FFFA):")
    for i = 0xFFFA, 0xFFFF do
        local val = mem:read_u8(i)
        io.write(string.format("%02X ", val))
    end
    
    print("\n--- END DUMP ---")
end

emu.add_machine_frame_notifier(function()
    frames = frames + 1
    -- Log PC occasionally
    if frames % 10 == 0 then
        -- print("Frame: " .. frames .. " PC: " .. cpu.state["PC"].value)
    end
    
    if frames >= TARGET_FRAMES then
        dump_state()
        manager.machine:exit()
    end
end)
