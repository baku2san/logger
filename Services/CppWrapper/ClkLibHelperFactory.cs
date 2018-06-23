using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace loggerApp.CppWrapper
{
    /// <summary>
    /// Factory って言いながら種類ないよ。上限管理だけやん？
    /// 基本、Openしっぱなしなので、Open上限管理をここに移譲しただけかな
    /// </summary>
    public class ClkLibHelperFactory
    {
        private List<int> unitAdrs;
        public Byte NetworkNo { get; set; }
        public Byte NodeNo { get; set; }
        public Byte UnitNo { get; set; }

        public ClkLibHelperFactory(Byte networkNo, Byte nodeNo, Byte unitNo)
        {
            unitAdrs = new List<int>();
            NetworkNo = networkNo;
            NodeNo = nodeNo;
            UnitNo = unitNo;
        }
        public ClkLibHelper GetHelper()
        {
            // 割当可能番号上限内で、未割り当ての番号を返す
            var currentUnitAdr = Enumerable.Range(1, ClkLibConstants.MutipleAccess).Except<int>(unitAdrs).FirstOrDefault();
            if (currentUnitAdr == 0)    // Default == 0 なら空きなし
            {
                Log.Error("ClkLib can not asign Unit Adr. current : {0}", unitAdrs.Count);
                return null;
            }
            else
            {
                unitAdrs.Add(currentUnitAdr);
                return new ClkLibHelper(currentUnitAdr, NetworkNo, NodeNo, UnitNo);
            }

        }
    }
}
