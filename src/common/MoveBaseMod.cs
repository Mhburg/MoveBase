using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoveBase
{
    public class MoveBaseMod : Mod
    {
        private static FieldInfo _rootdir = typeof(ModContentPack).GetField("rootDirInt", BindingFlags.NonPublic | BindingFlags.Instance);

        public static DateTime CreationTime;

        public static MoveBaseSetting Setting { get; private set; }

        public MoveBaseMod(ModContentPack content)
            : base(content)
        {
            Setting = this.GetSettings<MoveBaseSetting>();
            CreationTime = (_rootdir.GetValue(this.Content) as DirectoryInfo).CreationTimeUtc;
        }
    }
}
