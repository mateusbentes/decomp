using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Decomp.Core.Operators;
namespace Decomp.Core
{
    public enum GameMode
    {
        Caribbean = 3,
        WarbandScriptEnhancer450 = 2,
        WarbandScriptEnhancer320 = 1,
        Vanilla = 0
    }

    public static class Common
    {
        public static string ModuleConstantsText { get; set; } = @"from ID_animations import *
from ID_factions import *
from ID_info_pages import *
from ID_items import *
from ID_map_icons import *
from ID_menus import *
from ID_meshes import *
from ID_mission_templates import *
from ID_music import *
from ID_particle_systems import *
from ID_parties import *
from ID_party_templates import *
from ID_postfx_params import *
from ID_presentations import *
from ID_quests import *
from ID_scenes import *
from ID_scene_props import *
from ID_scripts import *
from ID_skills import *
from ID_sounds import *
from ID_strings import *
from ID_tableau_materials import *
from ID_troops import *";

        public static string ModuleConstantsVanillaText { get; set; } = @"from ID_animations import *
from ID_factions import *
from ID_items import *
from ID_map_icons import *
from ID_menus import *
from ID_meshes import *
from ID_mission_templates import *
from ID_music import *
from ID_particle_systems import *
from ID_parties import *
from ID_party_templates import *
from ID_presentations import *
from ID_quests import *
from ID_scenes import *
from ID_scene_props import *
from ID_scripts import *
from ID_skills import *
from ID_sounds import *
from ID_strings import *
from ID_tableau_materials import *
from ID_troops import *";

        public static GameMode SelectedMode { get; set; } = GameMode.WarbandScriptEnhancer450;
        public static bool IsVanillaMode => SelectedMode == GameMode.Vanilla;

        public static IReadOnlyList<string> Procedures { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> QuickStrings { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Strings { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Troops { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Factions { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Quests { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> PartyTemplates { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Parties { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Menus { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Sounds { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Skills { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Meshes { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Variables { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> DialogStates { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Scenes { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> MissionTemplates { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> ParticleSystems { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> SceneProps { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> MapIcons { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Presentations { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Tableaus { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Animations { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Music { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> Skins { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<string> InfoPages { get; set; } = Array.Empty<string>();

        public static string GetIdentifier(string prefix, IList<string> array, int index, bool useQuotes = false)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(prefix);

            if (index < 0 || index >= array.Count) return index.ToString(CultureInfo.InvariantCulture);
            var identifier = prefix + (prefix.Length > 0 && prefix[^1] == '_' ? "" : "_") + array[index];
            return useQuotes ? $"\"{identifier}\"" : identifier;
        }

        public static string GetIdentifier(string prefix, IList<string> array, ulong index, bool useQuotes = false)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(prefix);

            if (index >= (ulong)array.Count) return index.ToString(CultureInfo.InvariantCulture);
            var identifier = prefix + (prefix.Length > 0 && prefix[^1] == '_' ? "" : "_") + array[(int)index];
            return useQuotes ? $"\"{identifier}\"" : identifier;
        }

        public static string GetIdentifier(string prefix, IReadOnlyList<string> array, int index, bool useQuotes = false)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(prefix);

            if (index < 0 || index >= array.Count) return index.ToString(CultureInfo.InvariantCulture);
            var identifier = prefix + (prefix.Length > 0 && prefix[^1] == '_' ? "" : "_") + array[index];
            return useQuotes ? $"\"{identifier}\"" : identifier;
        }

        public static string GetIdentifier(string prefix, IReadOnlyList<string> array, ulong index, bool useQuotes = false)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(prefix);

            if (index >= (ulong)array.Count) return index.ToString(CultureInfo.InvariantCulture);
            var identifier = prefix + (prefix.Length > 0 && prefix[^1] == '_' ? "" : "_") + array[(int)index];
            return useQuotes ? $"\"{identifier}\"" : identifier;
        }

        public static string GetParameter(ulong parameter)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (parameter & tagMask) >> 56;
            var value = parameter & ~tagMask;

            return tag switch
            {
                1 => $"reg{(int)value}",
                2 => (int)value < Variables.Count ? $"\"${Variables[(int)value]}\"" : $"0x{parameter:x16}",
                3 => (int)value < Strings.Count ? $"\"str_{Strings[(int)value]}\"" : $"0x{parameter:x16}",
                4 => (int)value < Items.Count ? $"\"itm_{Items[(int)value]}\"" : $"0x{parameter:x16}",
                5 => (int)value < Troops.Count ? $"\"trp_{Troops[(int)value]}\"" : $"0x{parameter:x16}",
                6 => (int)value < Factions.Count ? $"\"fac_{Factions[(int)value]}\"" : $"0x{parameter:x16}",
                7 => (int)value < Quests.Count ? $"\"qst_{Quests[(int)value]}\"" : $"0x{parameter:x16}",
                8 => (int)value < PartyTemplates.Count ? $"\"pt_{PartyTemplates[(int)value]}\"" : $"0x{parameter:x16}",
                9 => (int)value < Parties.Count ? $"\"p_{Parties[(int)value]}\"" : $"0x{parameter:x16}",
                10 => (int)value < Scenes.Count ? $"\"scn_{Scenes[(int)value]}\"" : $"0x{parameter:x16}",
                11 => (int)value < MissionTemplates.Count ? $"\"mt_{MissionTemplates[(int)value]}\"" : $"0x{parameter:x16}",
                12 => (int)value < Menus.Count ? $"\"mnu_{Menus[(int)value]}\"" : $"0x{parameter:x16}",
                13 => (int)value < Procedures.Count ? $"\"script_{Procedures[(int)value]}\"" : $"0x{parameter:x16}",
                14 => (int)value < ParticleSystems.Count ? $"\"psys_{ParticleSystems[(int)value]}\"" : $"0x{parameter:x16}",
                15 => (int)value < SceneProps.Count ? $"\"spr_{SceneProps[(int)value]}\"" : $"0x{parameter:x16}",
                16 => (int)value < Sounds.Count ? $"\"snd_{Sounds[(int)value]}\"" : $"0x{parameter:x16}",
                17 => $":var{(int)value}",
                18 => (int)value < MapIcons.Count ? $"\"icon_{MapIcons[(int)value]}\"" : $"0x{parameter:x16}",
                19 => (int)value < Skills.Count ? $"\"skl_{Skills[(int)value]}\"" : $"0x{parameter:x16}",
                20 => (int)value < Meshes.Count ? $"\"mesh_{Meshes[(int)value]}\"" : $"0x{parameter:x16}",
                21 => (int)value < Presentations.Count ? $"\"prsnt_{Presentations[(int)value]}\"" : $"0x{parameter:x16}",
                22 => (int)value < QuickStrings.Count ? $"\"@{QuickStrings[(int)value]}\"" : $"0x{parameter:x16}",
                23 => (int)value < Music.Count ? $"\"track_{Music[(int)value]}\"" : $"0x{parameter:x16}",
                24 => (int)value < Tableaus.Count ? $"\"tableau_{Tableaus[(int)value]}\"" : $"0x{parameter:x16}",
                25 => (int)value < Animations.Count ? $"\"anim_{Animations[(int)value]}\"" : $"0x{parameter:x16}",
                _ => parameter.ToString(CultureInfo.InvariantCulture)
            };
        }

        public static string GetTriggerParameter(double dblParam) => GetTriggerParam(dblParam);

        public static string GetTriggerParam(double dblParam)
        {
            return (int)dblParam switch
            {
                -2 => "ti_on_game_start",
                -5 => "ti_simulate_battle",
                -6 => "ti_on_party_encounter",
                -8 => "ti_question_answered",
                -15 => "ti_server_player_joined",
                -16 => "ti_on_multiplayer_mission_end",
                -19 => "ti_before_mission_start",
                -20 => "ti_after_mission_start",
                -21 => "ti_tab_pressed",
                -22 => "ti_inventory_key_pressed",
                -23 => "ti_escape_pressed",
                -24 => "ti_battle_window_opened",
                -25 => "ti_on_agent_spawn",
                -26 => "ti_on_agent_killed_or_wounded",
                -27 => "ti_on_agent_knocked_down",
                -28 => "ti_on_agent_hit",
                -29 => "ti_on_player_exit",
                -30 => "ti_on_leave_area",
                -40 => "ti_on_scene_prop_init",
                -42 => "ti_on_scene_prop_hit",
                -43 => "ti_scene_prop_destroy",
                -44 => "ti_on_scene_prop_use",
                -45 => "ti_on_scene_prop_is_animating",
                -46 => "ti_on_scene_prop_animation_finished",
                -47 => "ti_on_scene_prop_start_use",
                -48 => "ti_on_scene_prop_cancel_use",
                -50 => "ti_on_init_item",
                -51 => "ti_on_weapon_attack",
                -52 => "ti_on_missile_hit",
                -53 => "ti_on_item_picked_up",
                -54 => "ti_on_item_dropped",
                -55 => "ti_on_agent_mount",
                -56 => "ti_on_agent_dismount",
                -57 => "ti_on_item_wielded",
                -58 => "ti_on_item_unwielded",
                -60 => "ti_on_presentation_load",
                -61 => "ti_on_presentation_run",
                -62 => "ti_on_presentation_event_state_change",
                -63 => "ti_on_presentation_mouse_enter_leave",
                -64 => "ti_on_presentation_mouse_press",
                -70 => "ti_on_init_map_icon",
                -71 => "ti_on_order_issued",
                -75 => "ti_on_switch_to_map",
                -76 => "ti_scene_prop_deformation_finished",
                -80 => "ti_on_shield_hit",
                -100 => "ti_on_scene_prop_stepped_on",
                -101 => "ti_on_init_missile",
                -102 => "ti_on_agent_turn",
                -103 => SelectedMode == GameMode.WarbandScriptEnhancer450 ? "ti_on_agent_blocked" : "ti_on_shield_hit",
                -104 => "ti_on_missile_dive",
                -105 => "ti_on_agent_start_reloading",
                -106 => "ti_on_agent_end_reloading",
                -107 => "ti_on_shield_penetrated",
                100000000 => "ti_once",
                _ => dblParam.ToString(CultureInfo.InvariantCulture)
            };
        }

        public static string GetIndentation(int indentationLevel) => new string(' ', Math.Max(indentationLevel, 0) * 2);

        public static void PrintStatement(ref Text input, ref FileWriter output, int recordCount, string defaultIndentation)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(output);

            var indentationLevel = 0;
            for (var i = 0; i < recordCount; i++)
            {
                var opcode = input.GetInt64();

                var isNegated = (opcode & 0x80000000) != 0;
                if (isNegated) opcode ^= 0x80000000;

                var isThisOrNext = (opcode & 0x40000000) != 0;
                if (isThisOrNext) opcode ^= 0x40000000;

                var op = FindOperator((int)(opcode & 0xFFFF));

                if (opcode is 4 or 6 or 7 or 11 or 12 or 15 or 16 or 17 or 18)
                    indentationLevel++;
                else if (opcode == 3)
                    indentationLevel--;

                var indentation = opcode is 4 or 5 or 6 or 7 or 11 or 12 or 15 or 16 or 17 or 18
                    ? GetIndentation(indentationLevel - 1)
                    : GetIndentation(indentationLevel);

                string? opcodeString = null;
                if (isNegated && opcode >= 30 && opcode <= 32)
                {
                    opcodeString = opcode switch
                    {
                        30 => "lt",
                        31 => "neq",
                        32 => "le",
                        _ => null
                    };
                    output.Write($"{indentation}{defaultIndentation}({(isThisOrNext ? "this_or_next|" : "")}{opcodeString}");
                }
                else
                {
                    opcodeString = op.Value;
                    output.Write($"{indentation}{defaultIndentation}({(isNegated ? "neg|" : "")}{(isThisOrNext ? "this_or_next|" : "")}{opcodeString}");
                }

                var parameterCount = input.GetInt();
                for (var p = 0; p < parameterCount; p++)
                {
                    var parameter = input.GetWord() ?? string.Empty;
                    output.Write($", {op.GetParameter(p, parameter)}");
                }
                output.WriteLine("),");
            }
        }

        public static Operator FindOperator(int operatorCode)
        {
            return Operators.TryGetValue(operatorCode, out var op) ? op : new Operator(operatorCode.ToString(CultureInfo.InvariantCulture), operatorCode);
        }

        public static IReadOnlyDictionary<int, Operator> Operators { get; set; } = new Dictionary<int, Operator>();

        public static string InputPath { get; set; } = string.Empty;
        public static string OutputPath { get; set; } = string.Empty;

        public static string GetKeyCode(ulong keyCode) => keyCode switch
        {
            0x02 => "key_1",
            0x03 => "key_2",
            0x04 => "key_3",
            0x05 => "key_4",
            0x06 => "key_5",
            0x07 => "key_6",
            0x08 => "key_7",
            0x09 => "key_8",
            0x0a => "key_9",
            0x0b => "key_0",
            0x1e => "key_a",
            0x30 => "key_b",
            0x2e => "key_c",
            0x20 => "key_d",
            0x12 => "key_e",
            0x21 => "key_f",
            0x22 => "key_g",
            0x23 => "key_h",
            0x17 => "key_i",
            0x24 => "key_j",
            0x25 => "key_k",
            0x26 => "key_l",
            0x32 => "key_m",
            0x31 => "key_n",
            0x18 => "key_o",
            0x19 => "key_p",
            0x10 => "key_q",
            0x13 => "key_r",
            0x1f => "key_s",
            0x14 => "key_t",
            0x16 => "key_u",
            0x2f => "key_v",
            0x11 => "key_w",
            0x2d => "key_x",
            0x15 => "key_y",
            0x2c => "key_z",
            0x52 => "key_numpad_0",
            0x4f => "key_numpad_1",
            0x50 => "key_numpad_2",
            0x51 => "key_numpad_3",
            0x4b => "key_numpad_4",
            0x4c => "key_numpad_5",
            0x4d => "key_numpad_6",
            0x47 => "key_numpad_7",
            0x48 => "key_numpad_8",
            0x49 => "key_numpad_9",
            0x45 => "key_num_lock",
            0xb5 => "key_numpad_slash",
            0x37 => "key_numpad_multiply",
            0x4a => "key_numpad_minus",
            0x4e => "key_numpad_plus",
            0x9c => "key_numpad_enter",
            0x53 => "key_numpad_period",
            0xd2 => "key_insert",
            0xd3 => "key_delete",
            0xc7 => "key_home",
            0xcf => "key_end",
            0xc9 => "key_page_up",
            0xd1 => "key_page_down",
            0xc8 => "key_up",
            0xd0 => "key_down",
            0xcb => "key_left",
            0xcd => "key_right",
            0x3b => "key_f1",
            0x3c => "key_f2",
            0x3d => "key_f3",
            0x3e => "key_f4",
            0x3f => "key_f5",
            0x40 => "key_f6",
            0x41 => "key_f7",
            0x42 => "key_f8",
            0x43 => "key_f9",
            0x44 => "key_f10",
            0x57 => "key_f11",
            0x58 => "key_f12",
            0x39 => "key_space",
            0x01 => "key_escape",
            0x1c => "key_enter",
            0x0f => "key_tab",
            0x0e => "key_back_space",
            0x1a => "key_open_braces",
            0x1b => "key_close_braces",
            0x33 => "key_comma",
            0x34 => "key_period",
            0x35 => "key_slash",
            0x2b => "key_back_slash",
            0x0d => "key_equals",
            0x0c => "key_minus",
            0x27 => "key_semicolon",
            0x28 => "key_apostrophe",
            0x29 => "key_tilde",
            0x3a => "key_caps_lock",
            0x2a => "key_left_shift",
            0x36 => "key_right_shift",
            0x1d => "key_left_control",
            0x9d => "key_right_control",
            0x38 => "key_left_alt",
            0xb8 => "key_right_alt",
            0xe0 => "key_left_mouse_button",
            0xe1 => "key_right_mouse_button",
            0xe2 => "key_middle_mouse_button",
            0xe3 => "key_mouse_button_4",
            0xe4 => "key_mouse_button_5",
            0xe5 => "key_mouse_button_6",
            0xe6 => "key_mouse_button_7",
            0xe7 => "key_mouse_button_8",
            0xee => "key_mouse_scroll_up",
            0xef => "key_mouse_scroll_down",
            0xf0 => "key_xbox_a",
            0xf1 => "key_xbox_b",
            0xf2 => "key_xbox_x",
            0xf3 => "key_xbox_y",
            0xf4 => "key_xbox_dpad_up",
            0xf5 => "key_xbox_dpad_down",
            0xf6 => "key_xbox_dpad_right",
            0xf7 => "key_xbox_dpad_left",
            0xf8 => "key_xbox_start",
            0xf9 => "key_xbox_back",
            0xfa => "key_xbox_rbumber",
            0xfb => "key_xbox_lbumber",
            0xfc => "key_xbox_ltrigger",
            0xfd => "key_xbox_rtrigger",
            0xfe => "key_xbox_rstick",
            0xff => "key_xbox_lstick",
            _ => $"0x{keyCode:x}"
        };

        public static string GetGameKey(ulong keyCode) => keyCode switch
        {
            0 => "gk_move_forward",
            1 => "gk_move_backward",
            2 => "gk_move_left",
            3 => "gk_move_right",
            4 => "gk_action",
            5 => "gk_jump",
            6 => "gk_attack",
            7 => "gk_defend",
            8 => "gk_kick",
            9 => "gk_toggle_weapon_mode",
            10 => "gk_equip_weapon_1",
            11 => "gk_equip_weapon_2",
            12 => "gk_equip_weapon_3",
            13 => "gk_equip_weapon_4",
            14 => "gk_equip_primary_weapon",
            15 => "gk_equip_secondary_weapon",
            16 => "gk_drop_weapon",
            17 => "gk_sheath_weapon",
            18 => "gk_leave",
            19 => "gk_zoom",
            20 => "gk_view_char",
            21 => "gk_cam_toggle",
            22 => "gk_view_orders",
            23 => "gk_order_1",
            24 => "gk_order_2",
            25 => "gk_order_3",
            26 => "gk_order_4",
            27 => "gk_order_5",
            28 => "gk_order_6",
            29 => "gk_everyone_hear",
            30 => "gk_infantry_hear",
            31 => "gk_archers_hear",
            32 => "gk_cavalry_hear",
            33 => "gk_group3_hear",
            34 => "gk_group4_hear",
            35 => "gk_group5_hear",
            36 => "gk_group6_hear",
            37 => "gk_group7_hear",
            38 => "gk_group8_hear",
            39 => "gk_reverse_order_group",
            40 => "gk_everyone_around_hear",
            41 => "gk_mp_message_all",
            42 => "gk_mp_message_team",
            43 => "gk_character_window",
            44 => "gk_inventory_window",
            45 => "gk_party_window",
            46 => "gk_quests_window",
            47 => "gk_game_log_window",
            48 => "gk_quick_save",
            49 => "gk_crouch",
            50 => "gk_order_7",
            51 => "gk_order_8",
            _ => $"0x{keyCode:x}"
        };

        public static bool IsStringRegister(ulong parameter)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (parameter & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsKey(ulong keyCode)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (keyCode & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsTextFlag(ulong flags)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (flags & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsPosition(ulong flags)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (flags & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsFloatRegister(ulong flags)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (flags & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsFaceKey(ulong flags)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (flags & tagMask) >> 56;
            return tag == 0;
        }

        public static bool IsNotParameter(ulong flags)
        {
            const ulong tagMask = 0xFF00000000000000;
            var tag = (flags & tagMask) >> 56;
            return tag == 0;
        }

        public static string GetFaceKey(ulong faceKeyCode) => faceKeyCode.ToString(CultureInfo.InvariantCulture);

        public static string DecompileTextFlags(uint flags)
        {
            var flagBuilder = new StringBuilder(32);

            string[] flagNames =
            {
                "tf_left_align", "tf_right_align", "tf_center_justify", "tf_double_space",
                "tf_vertical_align_center", "tf_scrollable", "tf_single_line", "tf_with_outline",
                "tf_scrollable_style_2"
            };
            uint[] flagValues =
            {
                0x00000004, 0x00000008, 0x00000010, 0x00000800, 0x00001000,
                0x00002000, 0x00008000, 0x00010000, 0x00020000
            };

            for (var i = 0; i < flagValues.Length; i++)
            {
                if ((flags & flagValues[i]) == 0) continue;
                flagBuilder.Append(flagNames[i]);
                flagBuilder.Append('|');
                flags ^= flagValues[i];
            }

            if (flagBuilder.Length == 0)
                flagBuilder.Append('0');
            else
                flagBuilder.Length--;

            return flagBuilder.ToString();
        }

        public static string GetAgentClass(ulong agentClass) => agentClass switch
        {
            0 => "grc_infantry",
            1 => "grc_archers",
            2 => "grc_cavalry",
            3 => "grc_infantry",
            9 => "grc_everyone",
            _ => agentClass.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetTeamOrder(ulong order) => order switch
        {
            0 => "mordr_hold",
            1 => "mordr_follow",
            2 => "mordr_charge",
            3 => "mordr_mount",
            4 => "mordr_dismount",
            5 => "mordr_advance",
            6 => "mordr_fall_back",
            7 => "mordr_stand_closer",
            8 => "mordr_spread_out",
            9 => "mordr_use_blunt_weapons",
            10 => "mordr_use_melee_weapons",
            11 => "mordr_use_ranged_weapons",
            12 => "mordr_use_any_weapon",
            13 => "mordr_stand_ground",
            14 => "mordr_fire_at_my_command",
            15 => "mordr_all_fire_now",
            16 => "mordr_left_fire_now",
            17 => "mordr_middle_fire_now",
            18 => "mordr_right_fire_now",
            19 => "mordr_fire_at_will",
            20 => "mordr_retreat",
            21 => "mordr_form_1_row",
            22 => "mordr_form_2_row",
            23 => "mordr_form_3_row",
            24 => "mordr_form_4_row",
            25 => "mordr_form_5_row",
            _ => order.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetPartyBehavior(ulong behavior)
        {
            var behaviorIndex = (int)behavior;
            string[] behaviorNames =
            {
                "ai_bhvr_hold", "ai_bhvr_travel_to_party", "ai_bhvr_patrol_location",
                "ai_bhvr_patrol_party", "ai_bhvr_attack_party", "ai_bhvr_avoid_party",
                "ai_bhvr_travel_to_point", "ai_bhvr_negotiate_party", "ai_bhvr_in_town",
                "ai_bhvr_travel_to_ship", "ai_bhvr_escort_party", "ai_bhvr_driven_by_party"
            };
            return behaviorIndex <= 11 ? behaviorNames[behaviorIndex] : behaviorIndex.ToString(CultureInfo.InvariantCulture);
        }

        public static string GetCharacterAttribute(ulong attribute) => attribute switch
        {
            0 => "ca_strength",
            1 => "ca_agility",
            2 => "ca_intelligence",
            3 => "ca_charisma",
            _ => attribute.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetWeaponProficiency(ulong proficiency) => proficiency switch
        {
            0 => "wpt_one_handed_weapon",
            1 => "wpt_two_handed_weapon",
            2 => "wpt_polearm",
            3 => "wpt_archery",
            4 => "wpt_crossbow",
            5 => "wpt_throwing",
            6 => "wpt_firearm",
            _ => proficiency.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetInventorySlot(ulong slot) => slot switch
        {
            0 => "ek_item_0",
            1 => "ek_item_1",
            2 => "ek_item_2",
            3 => "ek_item_3",
            4 => "ek_head",
            5 => "ek_body",
            6 => "ek_foot",
            7 => "ek_gloves",
            8 => "ek_horse",
            9 => "ek_food",
            _ => slot.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetTooltip(ulong tooltip) => tooltip switch
        {
            1 => "tooltip_agent",
            2 => "tooltip_horse",
            3 => "tooltip_my_horse",
            5 => "tooltip_container",
            6 => "tooltip_door",
            7 => "tooltip_item",
            8 => "tooltip_leave_area",
            9 => "tooltip_prop",
            10 => "tooltip_destructible_prop",
            _ => tooltip.ToString(CultureInfo.InvariantCulture)
        };

        public static string GetColor(ulong color)
        {
            if (color <= 0xFFFFFFFF && color > 0x00FFFFFF)
                return $"0x{color:X8}";
            if (color <= 0x00FFFFFF)
                return $"0x{color:X6}";
            return $"0x{color:X}";
        }

        public static string GetAlpha(ulong alpha) => $"0x{(alpha <= 0xFF ? alpha.ToString("X2") : alpha.ToString("X"))}";

        public static string DecompileSortMode(ulong sortMode)
        {
            return (sortMode & 3) switch
            {
                0x0 => "0",
                0x1 => "sort_f_desc",
                0x10 => "sort_f_ci",
                0x11 => "sort_f_ci | sort_f_desc",
                _ => sortMode.ToString(CultureInfo.InvariantCulture)
            };
        }

        public static bool GenerateIdEnabled { get; set; } = true;

        public static void GenerateId(string fileName, IEnumerable<string> content, string prefix = "")
        {
            if (!GenerateIdEnabled || string.IsNullOrEmpty(prefix) || content == null) return;

            var outputPath = Path.Combine(OutputPath, fileName);
            var enumerable = content.ToArray();
            using var writer = new StreamWriter(outputPath);
            if (prefix.Length > 0 && prefix[^1] != '_') prefix += '_';
            for (var i = 0; i < enumerable.Length; i++)
                writer.WriteLine($"{prefix}{enumerable[i]} = {i}");
        }

        public static string GetCommonIdentifier(string prefix, IReadOnlyList<string> array, int index, bool useQuotes = false)
        {
            if (index < 0 || index >= array.Count)
                return useQuotes ? $"\"{prefix}{index}\"" : $"{prefix}{index}";

            return useQuotes ? $"\"{prefix}{array[index]}\"" : $"{prefix}{array[index]}";
        }

        public static string GetCommonIdentifier(string prefix, IReadOnlyList<string> array, ulong index, bool useQuotes = false)
        {
            if (index >= (ulong)array.Count)
                return useQuotes ? $"\"{prefix}{index}\"" : $"{prefix}{index}";

            return useQuotes ? $"\"{prefix}{array[(int)index]}\"" : $"{prefix}{array[(int)index]}";
        }
    }
}
