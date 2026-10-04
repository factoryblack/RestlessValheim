using System;
using System.Linq;
using System.Text;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessWorks;

internal sealed partial class WorkshopWindow
{
    private void PaintProduction()
    {
        RestlessUi.PaperControl(_make.gameObject,_mode==WorksOrderMode.Make?RestlessUi.Accent:(Color?)null);
        RestlessUi.PaperControl(_keep.gameObject,_mode==WorksOrderMode.Keep?RestlessUi.Accent:(Color?)null);
        _modeHelp.text=_mode==WorksOrderMode.Make?"Produce this many additional units, then stop.":"Maintain this much usable stock. Refill as supplies are used.";
        Caption(_stationFilter,_station.Length==0?"All machines":Local(_recipes.FirstOrDefault(r=>r.Station==_station)?.StationName??_station));
        var rows=_recipes.Where(r=>Matches(r.OutputName)&&(_station.Length==0||r.Station==_station)).OrderBy(r=>r.OutputName).ToList();
        for(var i=0;i<rows.Count;i++)
        {
            var recipe=rows[i];var card=Get(_choices,i,_list.content);Place(card.Root,0,i*78,228,70);
            card.Set(Local(recipe.OutputName),"",Icon(recipe.Output),()=>{_output=recipe.Output;Refresh(true);Reset(_body);Reset(_details);},recipe.Output==_output,true);
        }
        Hide(_choices,rows.Count);Size(_list,rows.Count*78);
        var selected=_recipes.FirstOrDefault(r=>r.Output==_output);
        if(selected==null||_plan.Count==0){Empty("No matching production","Try another search or machine filter. Machine recipes become available when the world is loaded.");return;}
        var root=_plan[0];
        var usable=Math.Max(0,root.Available-_orders.Where(o=>o.Output==_output&&o.Mode==WorksOrderMode.Make).Sum(o=>o.Ready));
        var requested=_mode==WorksOrderMode.Make?_count:Math.Max(0,_count-usable);
        var title=(_mode==WorksOrderMode.Make?"Make ":"Keep ")+_count+" · "+Local(selected.OutputName);
        var hero=Get(_cards,0,_body.content);Place(hero.Root,0,4,536,148);
        hero.Set(title,_mode==WorksOrderMode.Make?"Additional output · existing finished stock is kept":usable+" usable now · target "+_count,Icon(_output),()=>{},true);
        if(_mode==WorksOrderMode.Keep)hero.Coverage(usable,0,_count);
        var itemCount=1;
        for(var i=0;i<root.Uses.Count;i++)
        {
            var use=root.Uses[i];var amount=(int)Math.Ceiling((double)use.Amount*requested/Math.Max(1,root.Need));
            var fuel=use.Item==selected.Fuel;var craftable=_recipes.Any(r=>r.Output==use.Item);
            var c=Get(_cards,itemCount++,_body.content);Place(c.Root,0,176+i*128,536,116);
            c.Set(Local(use.Name)+(craftable?"  ›":""),(fuel?"Fuel estimate: ":"Input: ")+amount+" needed · "+use.Available+" available"+(craftable?"\nSelect to order this separately":""),Icon(use.Item),()=>{if(craftable)PickOutput(use.Item);});
        }
        Hide(_cards,itemCount);Size(_body,176+root.Uses.Count*128);
        var matching=_machines.Where(m=>m.Prefab==selected.Station).ToList();
        var details=new StringBuilder("PRODUCTION REQUIREMENTS\n\n").AppendLine(Local(selected.StationName))
            .AppendLine(matching.Count+" machines in range").AppendLine(matching.Sum(m=>m.Free)+" free input slots").AppendLine()
            .AppendLine(_mode==WorksOrderMode.Make?"Make produces additional units.":"Keep counts usable stock after Make reservations, plus incoming production when the order is running.")
            .AppendLine().AppendLine("OUTPUT DESTINATION").AppendLine("Collected onto this board. Use Orders to collect Make output, or Output for unreserved stock.")
            .AppendLine().AppendLine("DEPENDENCIES").AppendLine("Input and fuel recipes can be ordered separately. This plan does not automatically queue them.");
        if(!string.IsNullOrEmpty(selected.Fuel))details.AppendLine().AppendLine("Fuel quantities are estimates. Machines retain their normal fuel use and processing time.");
        Detail(details.ToString());
        var duplicate=_orders.Any(o=>o.Output==_output&&o.Mode==WorksOrderMode.Keep);
        _hint.text=_mode==WorksOrderMode.Keep&&duplicate?"A Keep target already exists. Manage it in Orders.":_orders.Count>=8?"The board has eight orders. Remove or finish one first.":matching.Count==0?"No matching machine nearby. This order will wait.":_mode==WorksOrderMode.Keep&&requested==0?"Target already met. The order will monitor stock.":"Machines use available inputs and fuel. Missing supplies can be added later.";
        Caption(_action,(_mode==WorksOrderMode.Make?"Make ":"Keep ")+_count);
        _action.interactable=_orders.Count<8&&!(_mode==WorksOrderMode.Keep&&duplicate);
    }
    private void PaintOrders()
    {
        var orders=_orders.Where(o=>Matches(o.Name)).ToList();
        for(var i=0;i<orders.Count;i++)
        {
            var o=orders[i];var c=Get(_choices,i,_list.content);Place(c.Root,0,i*100,228,92);
            c.Set(Local(o.Name),o.Mode+" "+o.Count,Icon(o.Output),()=>{_order=o.Id;_confirmCancel=false;Refresh(true);Reset(_details);},o.Id==_order,true);
        }
        Hide(_choices,orders.Count);Size(_list,orders.Count*100);
        var order=orders.FirstOrDefault(o=>o.Id==_order);
        if(order==null){Empty("No orders selected","Place a Make order for a finite amount, or a Keep target to replenish stock automatically.");return;}
        var hero=Get(_cards,0,_body.content);Place(hero.Root,0,4,536,160);
        hero.Set(order.Mode+" "+order.Count+" · "+Local(order.Name),order.Mode==WorksOrderMode.Keep?"Stock coverage · green available, amber incoming":"Order completion · green finished, amber incoming",Icon(order.Output),()=>{},true);
        hero.Coverage(order.Available,order.InProduction,order.Count);
        var labels=order.Mode==WorksOrderMode.Keep?new[]{"Available stock","In production","Remaining shortage"}:new[]{"Finished","In production","Still to load"};
        var values=new[]{order.Available,order.InProduction,order.Remaining};
        for(var i=0;i<3;i++){var c=Get(_cards,i+1,_body.content);Place(c.Root,0,186+i*112,536,104);c.Set(labels[i]+"  "+values[i],i==0&&order.Mode==WorksOrderMode.Make?order.Ready+" ready on board · "+order.Collected+" collected":"",null,()=>{});}
        Hide(_cards,4);Size(_body,532);
        var recipe=_recipes.FirstOrDefault(r=>r.Output==order.Output);
        var matching=_machines.Where(m=>m.Prefab==recipe?.Station).ToList();
        var state=order.Mode==WorksOrderMode.Keep&&order.Available>=order.Count?"Target met · monitoring stock":order.InProduction>0?"Production underway":order.Ready>0&&order.Remaining==0?"Ready to collect":matching.Count==0?"Waiting for a matching machine":"Waiting for inputs, fuel or a free machine slot";
        var detail=new StringBuilder("ORDER #").Append(order.Id).Append("\n\n").AppendLine(state).AppendLine()
            .AppendLine(order.Mode==WorksOrderMode.Keep?"This target stays active and replenishes supplies as stock falls.":"This order stops after its requested additional output is made and collected.")
            .AppendLine().AppendLine("MACHINES").AppendLine(recipe==null?"Recipe unavailable":Local(recipe.StationName)).AppendLine(matching.Count+" in range")
            .AppendLine().AppendLine("Stopping an order removes its instruction. Material already inside machines continues processing.");
        Detail(detail.ToString());
        _hint.text="";Caption(_action,_confirmCancel?"Confirm removal":order.Mode==WorksOrderMode.Keep?"Stop keeping stock":"Cancel order");_action.interactable=true;
        _secondary.gameObject.SetActive(order.Mode==WorksOrderMode.Make);Caption(_secondary,"Collect "+order.Ready+" ready");
        _secondary.interactable=order.Ready>0&&Player.m_localPlayer!=null&&Player.m_localPlayer.GetPlayerID()==order.PlayerId;
        if(order.Mode==WorksOrderMode.Make&&Player.m_localPlayer!=null&&order.PlayerId!=Player.m_localPlayer.GetPlayerID())_hint.text="Only the player who placed this Make order can collect it.";
    }
    private void PaintMachines()
    {
        var rows=_machines.Select((m,i)=>new{Machine=m,Index=i}).Where(x=>Matches(x.Machine.Name)).ToList();
        if(!rows.Any(x=>x.Index==_machine))_machine=rows.FirstOrDefault()?.Index??0;
        for(var i=0;i<rows.Count;i++)
        {
            var row=rows[i];var c=Get(_choices,i,_list.content);Place(c.Root,0,i*100,228,92);
            c.Set(Local(row.Machine.Name),Mathf.RoundToInt(Vector3.Distance(_board.transform.position,row.Machine.Position))+" m from board",null,()=>{_machine=row.Index;Refresh(true);Reset(_details);},_machine==row.Index,true);
        }
        Hide(_choices,rows.Count);Size(_list,rows.Count*100);
        var machine=rows.FirstOrDefault(x=>x.Index==_machine)?.Machine;
        if(machine==null){Empty("No machines in range","Build a kiln, smelter or another supported production machine near the board.");return;}
        var hero=Get(_cards,0,_body.content);Place(hero.Root,0,4,536,148);
        hero.Set(Local(machine.Name),string.IsNullOrEmpty(machine.Block)?"Machine connected":machine.Block,null,()=>{},true);
        var counts=new[]{machine.Cooking+" inputs queued",machine.Ready+" processed output waiting",machine.Free+" free input slots"};
        for(var i=0;i<counts.Length;i++){var c=Get(_cards,i+1,_body.content);Place(c.Root,0,170+i*112,536,104);c.Set(counts[i],"",null,()=>{});}
        Hide(_cards,4);Size(_body,506);
        var distance=Vector3.Distance(_board.transform.position,machine.Position);
        Detail("MACHINE STATUS\n\n"+Local(machine.Name)+"\n"+distance.ToString("0.0")+" m from board\n\n"+(machine.FuelMax>0?"FUEL\n"+machine.Fuel+" / "+machine.FuelMax:"No fuel required")+"\n\n"+(string.IsNullOrEmpty(machine.Block)?"No reported block.":machine.Block)+"\n\nCounts are live machine queues. Processing time and machine conditions remain native.\n\nA finished unit drops at the machine.");
        _hint.text="Manage what this machine makes through Production and Orders.";_action.gameObject.SetActive(false);
    }
    private void PaintOutput()
    {
        var rows=_stock.Where(s=>Matches(s.Name)).OrderBy(s=>s.Name).ToList();
        for(var i=0;i<rows.Count;i++)
        {
            var item=rows[i];var c=Get(_choices,i,_list.content);Place(c.Root,0,i*100,228,92);
            c.Set(Local(item.Name),Math.Max(0,item.Count-item.Reserved)+" available",Icon(item.Prefab),()=>{_stockItem=item.Prefab;Refresh(true);Reset(_details);},item.Prefab==_stockItem,true);
        }
        Hide(_choices,rows.Count);Size(_list,rows.Count*100);
        var selected=rows.FirstOrDefault(s=>s.Prefab==_stockItem);
        if(selected==null){Empty("Board output is empty","Processed output collected for orders appears here. Make reservations are collected from Orders.");return;}
        var available=Math.Max(0,selected.Count-selected.Reserved);
        var hero=Get(_cards,0,_body.content);Place(hero.Root,0,4,536,148);hero.Set(Local(selected.Name),selected.Count+" stored on this board",Icon(selected.Prefab),()=>{},true);
        var a=Get(_cards,1,_body.content);Place(a.Root,0,176,536,116);a.Set(available+" available to take",selected.Reserved+" reserved for Make orders",Icon(selected.Prefab),()=>{});
        Hide(_cards,2);Size(_body,312);
        Detail("BOARD OUTPUT\n\nUnreserved materials can be taken into your inventory.\n\nMake output is reserved for its order. Collect it from Orders.\n\nKeep output is unreserved. Taking it into your bag may still count towards the Keep target while you remain nearby.\n\nAutomatic delivery to a designated chest is not connected yet.");
        _hint.text=available>0?"Take up to one stack. Anything that does not fit stays here.":"All of this item is reserved. Open its Make order to collect it.";
        Caption(_action,"Take a stack");_action.interactable=available>0;
    }
    private void Empty(string title,string description)
    {
        var card=Get(_cards,0,_body.content);Place(card.Root,0,4,536,168);card.Set(title,description,null,()=>{});Hide(_cards,1);Size(_body,180);
        Detail(description);_hint.text="";_action.gameObject.SetActive(false);_secondary.gameObject.SetActive(false);
    }
}
