using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorldUtility;
using Verse;

namespace MoveBase
{
    public class GameSaveComponent : GameComponent
    {
        private const int UpdateInterval = GenDate.TicksPerHour / 10;

        private static int _lastUpdateTick = 0;

        private Mod _mod;

        public GameSaveComponent(Game game)
        {
            _mod = LoadedModManager.GetMod<MoveBaseMod>();
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                DesignatorMoveBase.ClearCache();
                _lastUpdateTick = 0;
            }

            DesignatorMoveBase.ExposeData();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (_lastUpdateTick + UpdateInterval > Current.Game.tickManager.TicksGame)
            {
                return;
            }
            else
            {
                DesignatorMoveBase.PlaceWaitingBuildings();

                _lastUpdateTick += UpdateInterval;
                PerfProfile.OutputLog();
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            foreach (FeatureNews news in MoveBaseMod.Setting.FeatureNews)
            {
                if (!news.Received && news.ReleaseDate > MoveBaseMod.CreationTime)
                {
                    Find.LetterStack.ReceiveLetter(new FeatureUpdateLetter(news, _mod));
                }
            }
        }
    }
}
