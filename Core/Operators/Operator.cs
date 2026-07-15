using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Decomp.Core.Operators
{
#pragma warning disable CA1720 // Identifier contains type name
    public enum Parameter
    {
        None,
        FaceKeyRegister,
        FloatRegister,
        GameKeyCode,
        KeyCode,
        Position,
        String,
        InventorySlot,
        Tooltip,
        ToolTip = Tooltip,
        Color,
        Alpha,
        TextFlags,
        MenuFlags,
        TroopFlags,
        WeaponProficiency,
        CharacterAttribute,
        PartyFlags,
        AiBehavior,
        ItemProperty,
        ItemCapability,
        TroopIdentifier,
        ItemIdentifier,
        PartyIdentifier,
        AnimationIdentifier,
        ScenePropIdentifier,
        SceneIdentifier,
        FactionIdentifier,
        TableauMaterialIdentifier,
        TableauIdentifier = TableauMaterialIdentifier,
        QuestIdentifier,
        PartyTemplateIdentifier,
        InfoPageIdentifier,
        SkillIdentifier,
        MapIconIdentifier,
        MeshIdentifier,
        ItemType,
        SoundIdentifier,
        SoundFlags,
        ScriptIdentifier,
        ParticleSystemIdentifier,
        AttributeIdentifier,
        ItemModifier,
        MenuIdentifier,
        PresentationIdentifier,
        TrackIdentifier,
        MusicFlags,
        EquipmentOverrideFlags,
        MissionTemplateIdentifier,
        SceneFlags,
        SortMode,
        SkinIdentifier,
    }
#pragma warning restore CA1720 // Identifier contains type name

    public enum Mode
    {
        Caribbean,
        WarbandScriptEnhancer450,
        WarbandScriptEnhancer320,
        Vanilla,
    }

#pragma warning disable CA1716 // Identifiers should not match keywords
    public class Operator
    {
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public IReadOnlyDictionary<int, Parameter> Parameters { get; set; } = new Dictionary<int, Parameter>();

        public Operator(string name, int code)
        {
            Name = name;
            Code = code;
            Parameters = new Dictionary<int, Parameter>();
        }

        public Operator(string name, int code, params Parameter[] parameters)
        {
            Name = name;
            Code = code;
            var paramDict = new Dictionary<int, Parameter>();
            for (int i = 0; i < parameters.Length; i++)
            {
                paramDict[i] = parameters[i];
            }
            Parameters = paramDict;
        }

        public string GetParameter(int index, string? s)
        {
            if (s == null) return string.Empty;

            if (!ulong.TryParse(s, out var t))
                return s;

            if (!Parameters.TryGetValue(index, out var parameter))
                return s;

            return parameter switch
            {
                Parameter.None => s,
                Parameter.FaceKeyRegister => Common.GetFaceKey(t),
                Parameter.FloatRegister => $"fp{(int)t}",
                Parameter.GameKeyCode => Common.GetGameKey(t),
                Parameter.KeyCode => Common.GetKeyCode(t),
                Parameter.Position => $"pos{(int)t}",
                Parameter.String => $"s{(int)t}",
                Parameter.InventorySlot => Common.GetInventorySlot(t),
                Parameter.Tooltip => Common.GetTooltip(t),
                Parameter.Color => Common.GetColor(t),
                Parameter.Alpha => Common.GetAlpha(t),
                Parameter.TextFlags => Common.DecompileTextFlags((uint)t),
                Parameter.MenuFlags => Menus.DecompileFlags(t),
                Parameter.TroopFlags => Troops.DecompileFlags((uint)t),
                Parameter.WeaponProficiency => Common.GetWeaponProficiency(t),
                Parameter.CharacterAttribute => Common.GetCharacterAttribute(t),
                Parameter.PartyFlags => Parties.DecompileFlags((uint)t),
                Parameter.AiBehavior => Common.GetPartyBehavior(t),
                Parameter.ItemProperty => Items.DecompileFlags(t),
                Parameter.ItemCapability => Items.DecompileCapabilities(t),
                Parameter.TroopIdentifier => Common.GetCommonIdentifier("trp_", Common.Troops, (int)t),
                Parameter.ItemIdentifier => Common.GetCommonIdentifier("itm_", Common.Items, (int)t),
                Parameter.PartyIdentifier => Common.GetCommonIdentifier("p_", Common.Parties, (int)t),
                Parameter.AnimationIdentifier => Common.GetCommonIdentifier("anim_", Common.Animations, (int)t),
                Parameter.ScenePropIdentifier => Common.GetCommonIdentifier("spr_", Common.SceneProps, (int)t),
                Parameter.SceneIdentifier => Common.GetCommonIdentifier("scn_", Common.Scenes, (int)t),
                Parameter.FactionIdentifier => Common.GetCommonIdentifier("fac_", Common.Factions, (int)t),
                Parameter.TableauMaterialIdentifier => Common.GetCommonIdentifier("tableau_", Common.Tableaus, (int)t),
                Parameter.QuestIdentifier => Common.GetCommonIdentifier("qst_", Common.Quests, (int)t),
                Parameter.PartyTemplateIdentifier => Common.GetCommonIdentifier("pt_", Common.PartyTemplates, (int)t),
                Parameter.InfoPageIdentifier => Common.GetCommonIdentifier("ip_", Common.InfoPages, (int)t),
                Parameter.SkillIdentifier => Common.GetCommonIdentifier("skl_", Common.Skills, (int)t),
                Parameter.MapIconIdentifier => Common.GetCommonIdentifier("icon_", Common.MapIcons, (int)t),
                Parameter.MeshIdentifier => Common.GetCommonIdentifier("mesh_", Common.Meshes, (int)t),
                Parameter.ItemType => Items.DecompileType(t),
                Parameter.SoundIdentifier => Common.GetCommonIdentifier("snd_", Common.Sounds, (int)t),
                Parameter.SoundFlags => Sounds.DecompileFlags((uint)t),
                Parameter.ScriptIdentifier => Common.GetCommonIdentifier("script_", Common.Procedures, (int)t),
                Parameter.ParticleSystemIdentifier => Common.GetCommonIdentifier("psys_", Common.ParticleSystems, (int)t),
                Parameter.AttributeIdentifier => Troops.DecompileCharacterAttribute((uint)t),
                Parameter.ItemModifier => Items.DecompileModifier((uint)t),
                Parameter.MenuIdentifier => Common.GetCommonIdentifier("mnu_", Common.Menus, (int)t),
                Parameter.PresentationIdentifier => Common.GetCommonIdentifier("prsnt_", Common.Presentations, (int)t),
                Parameter.TrackIdentifier => Common.GetCommonIdentifier("track_", Common.Music, (int)t),
                Parameter.MusicFlags => Music.DecompileFlags((uint)t),
                Parameter.EquipmentOverrideFlags => MissionTemplates.DecompileAlterFlags((uint)t),
                Parameter.MissionTemplateIdentifier => Common.GetCommonIdentifier("mt_", Common.MissionTemplates, (int)t),
                Parameter.SceneFlags => Scenes.DecompileFlags((uint)t),
                Parameter.SortMode => Common.DecompileSortMode(t),
                Parameter.SkinIdentifier => Common.GetCommonIdentifier("tf_", Common.Skins, (int)t),
                _ => s
            };
        }

        public static IEnumerable<Operator> GetCollection(IEnumerable<Operator> operators)
        {
            return operators;
        }

        public static IEnumerable<Operator> GetCollection(Mode m)
        {
            return m switch
            {
                Mode.Caribbean => new CaribbeanVersion().GetOperators(),
                Mode.WarbandScriptEnhancer450 => new WarbandScriptEnhancer450Version().GetOperators(),
                Mode.WarbandScriptEnhancer320 => new WarbandScriptEnhancer320Version().GetOperators(),
                Mode.Vanilla => new VanillaVersion().GetOperators(),
                _ => throw new ArgumentOutOfRangeException(nameof(m), m, null)
            };
        }
    }
#pragma warning restore CA1716 // Identifiers should not match keywords

    public interface IGameVersion
    {
        IEnumerable<Operator> GetOperators();
    }
}
