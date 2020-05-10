using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoveBase
{
    public class GameSaveComponent : GameComponent
    {
        public GameSaveComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                DesignatorMoveBase.ClearCache();
            }

            DesignatorMoveBase.ExposeData();
        }
    }
}
