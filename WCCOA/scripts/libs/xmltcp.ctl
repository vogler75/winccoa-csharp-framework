#uses "CtrlXmlRpc"

int xmltcp_client_id = 0;
mapping xmltcp_client;

int xmltcp_send_counter = 0;
time xmltcp_send_start = 0;

const char SOH = 1;
const char STX = 2;
const char ETX = 3;
const char ACK = 6;  // acknowledge
const char NAK = 15; // neg. ack

//--------------------------------------------------------------------------------------------------------------------
mixed xoa_addClient(string sMethod, dyn_mixed daArgs, string user, string language, string ip)
{
  //check parameters
  if(dynlen(daArgs)<3)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  string host = daArgs[1]; if ( host=="*" ) host = ip;
  int    port = daArgs[2];
  bool   init = daArgs[3];
  
  int id = xmltcp_clientAdd(host, port, init);
  
  return makeDynInt(id);
}

//--------------------------------------------------------------------------------------------------------------------
mixed xoa_delClient(string sMethod, dyn_mixed daArgs, string user, string language, string ip)
{
  //check parameters
  if(dynlen(daArgs)<1)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  string id = daArgs[1];
  
  if ( xmltcp_clientDel(id) )
    return makeDynInt(0);
  else
    return makeDynInt(-1);
}


//--------------------------------------------------------------------------------------------------------------------
synchronized mixed xoa_dpQueryConnectSingle(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<4)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");  

  int id = daArgs[1];
  int key = daArgs[2];  
  string query = daArgs[3];
  bool answer = daArgs[4];
  
  DebugTN(sMethod, key);

  if ( !mappingHasKey(xmltcp_client, id) )
    return makeDynInt(-1); // client not connected

  if ( mappingHasKey(xmltcp_client[id]["queries"], key) )
    return makeDynInt(-2); // query already connected  
    
  mapping x;
  x["C"] = id;
  x["Q"] = query;
  
  switch ( sMethod ) 
  {
    case "xoa.dpQueryConnectSingle": x["W"] = "xmltcp_dpQueryConnectCB"; break;
    case "xoa.tagQueryConnectSingle": x["W"] = "xmltcp_tagQueryConnectCB"; break;    
    default:
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+"!");        
  }
  
  if ( dpQueryConnectSingle((string)x["W"], answer, makeDynAnytype((int)x["C"], key), (string)x["Q"]) == 0 )
  {
    xmltcp_client[id]["queries"][key]=x;
    return makeDynInt(0);
  }
  else
  {
    derr = getLastError();
    DebugTN(derr);    
    return makeDynInt(-1);
  }
}

//--------------------------------------------------------------------------------------------------------------------
synchronized mixed xoa_dpQueryDisconnect(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<2)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  //query 
  int id = daArgs[1];
  int key = daArgs[2];
  string query = daArgs[3];
  
  if ( !mappingHasKey(xmltcp_client, id) )
    return makeDynInt(-1); // client not connected

  if ( !mappingHasKey(xmltcp_client[id]["queries"], key) )
    return makeDynInt(-2); // query not connected  

  mapping x = xmltcp_client[id]["queries"][key];
  if ( dpQueryDisconnect((string)x["W"], makeDynAnytype((int)x["C"], key)) == 0 )
  {
    mappingRemove(xmltcp_client[id]["queries"], key);
    return makeDynInt(0);
  }
  else
  {
    derr = getLastError();   
    DebugTN(derr);        
    return makeDynInt(-3);
  }
}

//--------------------------------------------------------------------------------------------------------------------
synchronized mixed xoa_dpConnect(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<4)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");  

  int id = daArgs[1];
  int key = daArgs[2]; 
  dyn_string dps = daArgs[3];
  bool answer = daArgs[4];

  DebugTN(sMethod, key, dps);
 
  if ( !mappingHasKey(xmltcp_client, id) )
    return makeDynInt(-1); // client not connected

  if ( mappingHasKey(xmltcp_client[id]["connects"], key) )
    return makeDynInt(-2); // connect already connected  
    
  mapping x;
  x["C"] = id;
  x["D"] = dps;
  
  switch ( sMethod ) 
  {
    case "xoa.dpConnect": x["W"] = "xmltcp_dpConnectCB"; break;
    case "xoa.tagConnect": x["W"] = "xmltcp_tagConnectCB"; break;    
    default:
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+"!");        
  }
  
  if ( dpConnectUserData((string)x["W"], makeDynAnytype((int)x["C"], key), answer, (dyn_string)x["D"]) == 0 )
  {
    xmltcp_client[id]["connects"][key]=x;
    return makeDynInt(0);
  }
  else
  {
    derr = getLastError();
    DebugTN(derr);    
    return makeDynInt(-1);
  }
}

//--------------------------------------------------------------------------------------------------------------------
synchronized mixed xoa_dpDisconnect(string sMethod, dyn_mixed daArgs, string user, string language)
{
  dyn_errClass derr;
  
  //check parameters
  if(dynlen(daArgs)<2)
    return xmlRpcMakeError(PRIO_SEVERE, ERR_CONTROL, ERR_ILLEGAL_FUNCTION_CALL, ""+sMethod+" no argument!");
  
  //query 
  int id = daArgs[1];
  int key = daArgs[2];   
  dyn_string dps = daArgs[3];
   
  if ( !mappingHasKey(xmltcp_client, id) )
    return makeDynInt(-1); // client not connected

  if ( !mappingHasKey(xmltcp_client[id]["connects"], key) )
    return makeDynInt(-2); // query not connected  

  mapping x = xmltcp_client[id]["connects"][key];
  if ( dpDisconnectUserData((string)x["W"], makeDynAnytype((int)x["C"], key), (dyn_string)x["D"]) == 0 )
  {
    mappingRemove(xmltcp_client[id]["connects"], key);
    return makeDynInt(0);
  }
  else
  {
    return makeDynInt(-3);
  }
}

//--------------------------------------------------------------------------------------------------------------------
void xmltcp_dpQueryConnectCB(dyn_anytype data, dyn_dyn_anytype res)
{
  xmltcp_send(data[1], "DpQueryConnectCB", makeDynAnytype(data[1], data[2], res));
}

void xmltcp_tagQueryConnectCB(dyn_anytype data, dyn_dyn_anytype res)
{
  xmltcp_send(data[1], "TagQueryConnectCB", makeDynAnytype(data[1], data[2], res));
  for ( int i=2; i<=dynlen(res); i++ )
  {
    if ( dynlen(res[i]) < 4 )
      DebugTN(res);
  }
}

void xmltcp_dpConnectCB(dyn_anytype data, dyn_string dps, dyn_anytype val)
{
  xmltcp_send(data[1], "DpConnectCB", makeDynAnytype(data[1], data[2], dps, val));
}

void xmltcp_tagConnectCB(dyn_anytype data, dyn_string dpe, dyn_anytype val)
{
  dyn_string dps;
  for ( int i=1; i<=dynlen(dpe); i++ )
    dynAppend(dps, dpSubStr(dpe[i], DPSUB_SYS_DP_EL));
  dynUnique(dps);
    
  xmltcp_send(data[1], "TagConnectCB", makeDynAnytype(data[1], data[2], dps, val));
}

//--------------------------------------------------------------------------------------------------------------------
int xmltcp_clientAdd(string host, int port, bool init)
{
  DebugTN("xmltcp_clientAdd", host, port, init);        
  synchronized ( xmltcp_client )
  {
    int id = -1;
    bool new = true;
    
    /*
    dyn_int keys = mappingKeys(xmltcp_client);
    for ( int i=1; i<=dynlen(keys); i++ )
    {
      if ( xmltcp_client[keys[i]]["host"] == host && 
           xmltcp_client[keys[i]]["port"] == port )
      {
        id = keys[i];
        new = false;
      }
    }
    */

    if ( new )
    {
      id = ++xmltcp_client_id;
    }    
    else if ( init )
    {
      xmltcp_clientReset(id);
    }
   
    if ( new || init ) 
    {
      mapping client, empty;
      client["host"]     = host;
      client["port"]     = port;
      client["socket"]   = (int)-1;
      client["time"]     = getCurrentTime();
      client["queries"]  = empty; 
      client["connects"] = empty;     
      xmltcp_client[id]  = client;      
    }
    
    if ( new ) 
    {
      DebugTN("xmltcp_clientAdd", host, port, init, id);      
      startThread("xmltcp_alive", id);
    }
    
    return id;
  }
}

int xmltcp_clientDel(int id)
{
  synchronized ( xmltcp_client )
  {
    if ( mappingHasKey(xmltcp_client, id) )
    {
      DebugTN("xmptcp_clientDel", id);
      xmltcp_clientReset(id);
      mappingRemove(xmltcp_client, id);
      return true;
    }
    else
      return false;      
  }    
}

int xmltcp_clientReset(int id)
{
  synchronized ( xmltcp_client )
  {
    if ( mappingHasKey(xmltcp_client, id) )
    {
      if ( xmltcp_client[id]["socket"] != -1 )
        tcpClose(xmltcp_client[id]["socket"]);      
      
      dyn_int keys;
      DebugTN("xmltcp_clientReset", id);
      //DebugTN("xmltcp_clientReset", "queries", id, xmltcp_client[id]["queries"]);
      keys = mappingKeys(xmltcp_client[id]["queries"]);
      for ( int i=1; i<=dynlen(keys); i++ )
      {
        mapping x = xmltcp_client[id]["queries"][keys[i]];
        dpQueryDisconnect((string)x["W"], makeDynAnytype((int)x["C"], keys[i]));
      }
      
      //DebugTN("xmltcp_clientReset", "connects", id, xmltcp_client[id]["connects"]);
      keys = mappingKeys(xmltcp_client[id]["connects"]);
      for ( int i=1; i<=dynlen(keys); i++ )
      {
        mapping x = xmltcp_client[id]["connects"][keys[i]];
        dpDisconnectUserData((string)x["W"], makeDynAnytype((int)x["C"], keys[i]), (dyn_string)x["D"]);
      }      
    }
  }  
}

//--------------------------------------------------------------------------------------------------------------------
void xmltcp_alive(int id)
{
  mapping client;
  while ( mappingHasKey(xmltcp_client, id) ) 
  {    
    /*
    if ( xmltcp_client[id]["time"] < getCurrentTime() - 3 )
    {
      DebugTN("alive timeout, removing connection " + client);
      xmltcp_clientDel(id);
    }
    else    
    */
    if ( xmltcp_client[id]["time"] < getCurrentTime() - 5 )
    {
      string s= getCurrentTime();
      xmltcp_send(id, "Alive", makeDynAnytype(id, "Alive XML Control " + s));
    }    
    delay(1);    
  }
}

//--------------------------------------------------------------------------------------------------------------------
synchronized bool xmltcp_send(int id, string methodName, const anytype &data)
{
  int ret;
  dyn_errClass err;           
  bool done = false; 
  string response;    
  char c;
   
  //DebugTN("xmltcp_send...start", methodName, id);  
  
  if ( ! mappingHasKey(xmltcp_client, id) ) return;
  
  int socket  = xmltcp_client[id]["socket"];
  string host = xmltcp_client[id]["host"];
  int port    = xmltcp_client[id]["port"];
  
  string xml = xmltcp_EncodeRequest(methodName, data);
  
  for ( int i=1; i<=1 && !done; i++ )
  {
    if ( ! mappingHasKey(xmltcp_client, id) ) return;      
    
    done=false;
    
    // open tcp connection
    if ( socket < 0 )
    {
      //DebugTN("xmltcp_send...tcpOpen...", host, port);
      socket=tcpOpen(host, port);
      err=getLastError();         
      if ( dynlen(err) > 0 )
        socket=-1;
    }    
    
    // if connection is available send data
    c=0;
    if ( socket >= 0 )
    {    
      //DebugTN("xmltcp_send", "SOH...");
      ret=tcpWrite(socket, SOH);
      err=getLastError();
      if ( dynlen(err) >  0 ) socket=-1;     
      else
      {
        ret=tcpRead(socket, response, 10);      
        if ( strlen(response) > 0 )
          c = response[0];               
      }            
      //DebugTN("xmltcp_send", "SOH..." + (int)c);
      
      if ( c == ACK ) 
      {      
        //DebugTN("xmltcp_send", "STX+xml+ETX...");        
        ret=tcpWrite(socket, STX+xml+ETX);  
        err=getLastError();    
        if ( dynlen(err) > 0 ) socket=-1;
        else 
        {
          // wait for response
          ret=tcpRead(socket, response, 10);      
          if ( strlen(response) > 0 )
          {
            c = response[0];
            if ( c == ACK ) done=true;
          }        
        }
        //DebugTN("xmltcp_send", "STX+xml+ETX...", (int)c);        
      }  
    }

    if ( !done || i > 1 ) 
      DebugTN(host+":"+port+":" + socket + " ret: " + ret + " done: " + done + " answer: " + (int)c + " i: " + i);   
  } 

  if ( ! mappingHasKey(xmltcp_client, id) ) return;  
  
  synchronized ( xmltcp_client ) 
  {
    xmltcp_client[id]["socket"]=socket; 
    if ( done ) 
      xmltcp_client[id]["time"]=getCurrentTime();
    else
      xmltcp_clientDel(id);
  }
  
  //DebugTN("xmltcp_send...done", methodName, id, done);  
  //DebugTN(id+" "+host+":"+port+":" + socket + " ret: " + ret + " done: " + done + " answer: " + answer + " "  + (float)(t2-t1));   
  
  xmltcp_send_counter++;
  if ( xmltcp_send_start == 0 )
    xmltcp_send_start = getCurrentTime();
  
  time t = getCurrentTime();
  if ( (t - xmltcp_send_start) > 5.0 )
  {
    DebugTN("xmltcp_send", xmltcp_send_counter, xmltcp_send_counter / (float)(t - xmltcp_send_start));
    xmltcp_send_start = t;
    xmltcp_send_counter = 0;
  }
  
  return done;
}

//--------------------------------------------------------------------------------------------------------------------
string xmltcp_EncodeRequest(string methodName, const anytype &param)
{
  string xml;
  xmlrpcEncodeValue(param, xml);
  return "<?xml version=\"1.0\"?>"
         "<methodCall>"
         "<methodName>"+methodName+"</methodName>"
         "<params>"
         "<param>" + xml + "</param>"
         "</params>"
         "</methodCall>";
}

