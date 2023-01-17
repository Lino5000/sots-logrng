using Type = System.Type;
using Environment = System.Environment;
using BepInEx;
using HarmonyLib;

namespace LogRngCalls
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        /*
        We want to hook:
        * System.Random (used by ink)
        ** ctor
        ** Next()

        * UnityEngine.Random
        ** InitState
        ** state {get; set;}
        ** value {get;}
        ** Range

        We *might* want to hook: 
        * EchoDog.SaveManager
        ** ImportantRandomStart
        ** ImportantRandomEnd
        ** ImportantRandomValue
        ** ImportantRandomElement

        * EchoDog.Utilities
        ** PushRandomState
        ** PopRandomState
        */

        static Plugin instance;

        static bool isCurrentlyImportantTime = false;

        private void Awake()
        {
            instance = this;
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo("ohai");

        }

        void LogRngCall()
        {
            if(isCurrentlyImportantTime) {
                Logger.LogInfo(Environment.StackTrace);
            }
        }


        // there is probably a cleaner way to do this than to hit it with a ton of boilerplate
        // especially since it's all doing the same thing but this seems the most straightforward
        // this is certainly better than giving each patch its own class though
        [HarmonyPatch(typeof(UnityEngine.Random))]
        private static class UnityRandomPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch("InitState")]
            static void InitStatePost(int seed) { instance.LogRngCall(); }

            [HarmonyPostfix]
            [HarmonyPatch("Range", new Type[] { typeof(float), typeof(float) })]
            static void FloatRangePost(float min, float max, float __result) { instance.LogRngCall(); }

            [HarmonyPostfix]
            [HarmonyPatch("Range", new Type[] { typeof(int), typeof(int) })]
            static void IntRangePost(int min, int max, int __result) { instance.LogRngCall(); }

            //NB: UnityEngine.Random.State looks like this:
            /*
            [Serializable]
	        public struct State
	        {
                [SerializeField]
                private int s0;

                [SerializeField]
                private int s1;

                [SerializeField]
                private int s2;

                [SerializeField]
                private int s3;
            }
            */
            [HarmonyPostfix]
            [HarmonyPatch("state", MethodType.Getter)]
            static void GetStatePost(UnityEngine.Random.State __result) { instance.LogRngCall(); }
            [HarmonyPostfix]
            [HarmonyPatch("state", MethodType.Setter)]
            static void SetStatePost(UnityEngine.Random.State value) { instance.LogRngCall(); }

            [HarmonyPostfix]
            [HarmonyPatch("value", MethodType.Getter)]
            static void GetValuePost(float __result) { instance.LogRngCall(); }

        }

        [HarmonyPatch(typeof(Echodog.SaveManager))]
        private static class EchoDogSaveManagerPatches
        {
            //start logging rng calls after ImportantRandomStart, after it's done its thing
            [HarmonyPostfix]
            [HarmonyPatch("ImportantRandomStart")]
            static void PostImportantStart() { isCurrentlyImportantTime = true; }

            //stop logging rng calls *before* ImportantRandomEnd, since it does rng stuff that we don't want to see
            [HarmonyPrefix]
            [HarmonyPatch("ImportantRandomEnd")]
            static void PostImportantEnd() { isCurrentlyImportantTime = false; }

        }
    }
}
