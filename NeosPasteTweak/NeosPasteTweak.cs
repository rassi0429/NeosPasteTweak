using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Collections.Generic;
using System;
using CodeX;
using BaseX;
using System.IO;

namespace NeosPasteTweak
{
    public class NeosPasteTweak : NeosMod
    {
        public override string Name => "NeosPasteTweak";
        public override string Author => "kka429";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/rassi0429/NeosPasteTweak"; // this line is optional and can be omitted

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> PREVENT_DOUBLE_PASTE_KEY = new("prevent_double_paste", "Prevents pasting file paths and folder links as text viewers / urls", ()=>true);

        public static ModConfiguration config;

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("dev.kokoa.neospastetweak");
            harmony.PatchAll();
        }

        struct PosRot
        {
            public float3 pos;
            public floatQ rot;

            public PosRot(float3 pos, floatQ rot)
            {
                this.pos = pos;
                this.rot = rot;
            }
        }

        [HarmonyPatch(typeof(BatchFolderImporter), "BatchImport", new Type[] { typeof(Slot), typeof(IEnumerable<string>), typeof(bool) })]
        class Patch
        {
            static void Prefix(Slot root, IEnumerable<string> files, bool forceUnknown, ref PosRot __state)
            {
                __state = new PosRot(root.GlobalPosition, root.GlobalRotation);
            }

            static void Postfix(Slot root, IEnumerable<string> files, bool forceUnknown, PosRot __state)
            {
                List<string> list = new List<string>();
                foreach (string str in files)
                {
                    AssetClass key = AssetHelper.IdentifyClass(str);
                    if(key == AssetClass.Unknown)
                    {
                        Uri uri;
                        if (Uri.TryCreate(str, UriKind.Absolute, out uri))
                        {
                            if (config.GetValue(PREVENT_DOUBLE_PASTE_KEY) && (uri.Scheme == "neosrec" || uri.Scheme == "file")) return;
                            string json = Items.LINK_VIEWER;
                            DataTreeDictionary urlObject = DataTreeConverter.FromJSON(json);

                            Slot newSlot = root.World.LocalUserSpace.AddSlot("URL");
                            newSlot.LoadObject(urlObject);
                            newSlot.GlobalPosition = __state.pos;
                            newSlot.GlobalRotation = __state.rot;
                            newSlot.GetComponent<ValueField<string>>().Value.Value = uri.ToString();
                        }
                        else if(!(config.GetValue(PREVENT_DOUBLE_PASTE_KEY) && File.Exists(str)))
                        {
                            string json = Items.TEXT_VIEWER;
                            DataTreeDictionary textObject = DataTreeConverter.FromJSON(json);
                            Slot newSlot = root.World.LocalUserSpace.AddSlot("Text");
                            newSlot.LoadObject(textObject);
                            newSlot.GlobalPosition = __state.pos;
                            newSlot.GlobalRotation = __state.rot;
                            newSlot.GetComponent<ValueField<string>>().Value.Value = str;
                        }
                    }
                }
            }
        }
    }
}