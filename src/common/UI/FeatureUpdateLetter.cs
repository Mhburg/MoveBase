using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorldUtility;
using RimWorldUtility.UI;
using Verse;

namespace MoveBase
{
    public class FeatureUpdateLetter : ChoiceLetter
    {
        private FeatureNews _news;

        private Mod _mod;

        public FeatureUpdateLetter()
        {
            this.def = MoveBaseDefOf.NotooShabbyFeatureUpdate;
        }

        public FeatureUpdateLetter(FeatureNews featureNews, Mod mod)
            : this()
        {
            _news = featureNews;
            _mod = mod;
            this.label = _news.Label;
            this.ID = Find.UniqueIDsManager.GetNextLetterID();
        }


        public override bool CanDismissWithRightClick
        {
            get
            {
                this.UpdateModSetting();
                return _news.Received = true;
            }
        }

        public override IEnumerable<DiaOption> Choices => new List<DiaOption>();

        public override void OpenLetter()
        {
            Find.WindowStack.Add(new Dialog_Feature(_news));
            _news.Received = true;
            this.UpdateModSetting();
        }

        protected override string GetMouseoverText()
        {
            return _news.Description;
        }

        private void UpdateModSetting()
        {
            _mod.WriteSettings();
        }
    }
}
