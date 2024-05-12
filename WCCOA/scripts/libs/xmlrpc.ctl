//--------------------------------------------------------------------------------------------------------------------
//--------------------------------------------------------------------------------------------------------------------
//--------------------------------------------------------------------------------------------------------------------

//--------------------------------------------------------------------------------------------------------------------
// 1... Zone... xoaHomescreen, xoaValues, xoaCommands, ...
mixed xoa_getGroups (string sMethod, dyn_mixed daArgs, string user, string language)
{
  string dpefilter = daArgs[1]+"_*"; //* fuer patternmatch
  
  dyn_anytype data, dps;
  string group, dp;  

  DebugTN("xoa_getGroups", dpefilter);    
  dps = groupGetNames();  
  for(int i=1; i<=dynlen(dps);i++)
  {         
    dp = getCurrentLanguageData(dps[i], language);
    if(patternMatch(dpefilter, dp))
    {  
      group=strltrim(dp, dpefilter+"_");     
      dynAppend(data, group);
    }      
  } 
  return data;    
}

//--------------------------------------------------------------------------------------------------------------------
// 1... Zone... xoaHomescreen, xoaValues, xoaCommands,...
// 2... Groupname
mixed xoa_getGroupItems(string sMethod, dyn_mixed daArgs, string user, string language)
{
  string zone = daArgs[1];
  string groupname = dynlen(daArgs) > 1 ? "_"+daArgs[2] : "";
  
  string dpefilter = zone + groupname; //ioa_cmd_Rauchmelder oder ioa_value_Becken  
  
  dyn_anytype data;
  dyn_anytype dps;

  DebugTN ("xoa_getGroupItems", dpefilter);
 
  dps = dpNames ("_DpGroup*","_DpGroup");  
  for(int i=1; i<=dynlen(dps);i++)
  {
    if ( dpefilter == dpGetDescription (dps[i]+".") )
    {
      dyn_string tmp;
      dpGet (dps[i]+".Dps:_online.._value", tmp);
      for ( int j=1; j<=dynlen(tmp); j++ )
        dynAppend(data, dpNames(tmp[j]));
      return data;
    }    
  }
  return data;     
}

//--------------------------------------------------------------------------------------------------------------------
mixed xoa_getTag(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<1)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  //get tag data
  string what = (dynlen(daArgs)==2) ? daArgs[2] : " ";
    
  dyn_anytype res = xoa_getTagValues(makeDynString(daArgs[1]), what, language);
  
  return dynlen(res) > 0 ? res[1] : "";                        
}

//--------------------------------------------------------------------------------------------------------------------
mixed xoa_getTags(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<1)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  //get tag data  
  string what = (dynlen(daArgs)==2) ? daArgs[2] : " ";
    
  dyn_anytype res = xoa_getTagValues(daArgs[1], what, language);
  
  return res;                        
}

//--------------------------------------------------------------------------------------------------------------------
time xoa_getTag_tEnd = getCurrentTime();
dyn_anytype xoa_getTagValues(dyn_string dps, string what, string language)
{

  time tStart = getCurrentTime();
  
  int i;
  dyn_anytype tmp, res_all;
  time ts;  
  
  for ( i=1; i<=dynlen(dps); i++ )
  {
    string dp = dps[i];
    dyn_anytype res;  
    
    // configs
    if ( what == " " || what == "C" )
    {
      dynClear(tmp);  

      dynAppend(tmp, getCurrentLanguageData(dpGetDescription(dp, 0), language)); // Desc
      dynAppend(tmp, getCurrentLanguageData(dpGetUnit(dp), language)); // Unit
      dynAppend(tmp, getCurrentLanguageData(dpGetFormat(dp), language));
  
      // value range
      int type;
      float min, max;
      dpGet(dp+":_pv_range.._type", type);
      if ( type == DPCONFIG_MINMAX_PVSS_RANGECHECK )
        dpGet(dp+":_pv_range.._min", min,
              dp+":_pv_range.._max", max);
      else
      {
        type=0;
        min=0;
        max=0;
      }
      dynAppend(tmp, type);
      dynAppend(tmp, min);
      dynAppend(tmp, max);
  
      // add to result
      res[dynlen(res)+1]=tmp;
    }

    if ( what == " " || what == "V" || what == "X" )
    {  
      // value
      dynClear(tmp);
      anytype val;
      bool inv, def, unc;
      dpGet(dp+":_online.._value", val,
            dp+":_online.._stime", ts,
            dp+":_online.._invalid", inv,
            dp+":_online.._default", def,
            dp+":_online.._uncertain", unc);
      dynAppend(tmp, val);
      dynAppend(tmp, ts);
      dynAppend(tmp, inv);
      dynAppend(tmp, def);
      dynAppend(tmp, unc);
  
      // add to result
      res[dynlen(res)+1]=tmp;
    }
  
    if ( what == " " || what == "A" || what == "X" )
    {
      // alert
      dynClear(tmp);
      
      langString text0, text1, text;
      string color;
      int state, prior;
  
      // TODO 
      dpGet(dps[i]+":_alert_hdl.._text0", text0);
      dpGet(dps[i]+":_alert_hdl.._text1", text1);
      dpGet(dps[i]+":_alert_hdl.._act_state", state);
      dpGet(dps[i]+":_alert_hdl.._text", text);
      dpGet(dps[i]+":_alert_hdl.._act_state_color", color);
      dpGet(dps[i]+":_alert_hdl.._prior", prior);      
            
      dynAppend(tmp, getCurrentLanguageData(text0, language));
      dynAppend(tmp, getCurrentLanguageData(text1, language));
      dynAppend(tmp, state);
      dynAppend(tmp, getCurrentLanguageData(text, language));      
      dynAppend(tmp, color);
      dynAppend(tmp, prior);
      
      // add to result
      res[dynlen(res)+1]=tmp; 
    }
    res_all[i]=res;
  }

  DebugTN("xoa_getTags " + dynlen(dps) + " " + (float)(getCurrentTime()-tStart) + " " + (float)(tStart-xoa_getTag_tEnd));
  xoa_getTag_tEnd=getCurrentTime();
  
  return res_all;
}

//--------------------------------------------------------------------------------------------------------------------
mixed xoa_waitForTags(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<1)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  //wait/get tag data  
  dyn_anytype res = xoa_waitForTagValues(daArgs[1], language);
  
  return res;                        
}

//--------------------------------------------------------------------------------------------------------------------
dyn_anytype xoa_waitForTagValues(dyn_string dps, string language)
{
  int i, j;
  dyn_anytype tmp, res_all;
  time ts;  
  
  dyn_string dpsWait;
  dyn_anytype cond;
  dyn_string dpsRet;
  dyn_anytype values; 
  
  for ( i=1; i<=dynlen(dps); i++ )
  {
    string dp = dps[i];
  
    dynAppend(dpsWait, dp+":_online.._value");
    
    dynAppend(dpsRet, dp+":_online.._value");
    dynAppend(dpsRet, dp+":_online.._stime");
    dynAppend(dpsRet, dp+":_online.._invalid");
    dynAppend(dpsRet, dp+":_online.._default");    
    dynAppend(dpsRet, dp+":_online.._uncertain");        
  }
  
  if ( dpWaitForValue(dpsWait, cond, dpsRet, values, 5) == 0 && dynlen(values) > 0 )
  { 
    j=0;
    for ( i=1; i<=dynlen(dps); i++ )
    {
      dyn_anytype res;  
      dynClear(tmp);
      dynAppend(tmp, values[++j]); // value
      dynAppend(tmp, values[++j]); // stime
      dynAppend(tmp, values[++j]); // invalid
      dynAppend(tmp, values[++j]); // default
      dynAppend(tmp, values[++j]); // uncertain
  
      // add to result
      res[dynlen(res)+1]=tmp;

      res_all[i]=res;
    }
  }
  DebugTN("xoa_waitForTags " + dynlen(dps) + " " + dynlen(values));
  
  return res_all;
}
