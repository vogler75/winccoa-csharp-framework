#uses "CtrlHTTP"
#uses "CtrlXmlRpc"
#uses "xmlrpcHandlerCommon.ctl"
#uses "CtrlZlib"
#uses "dpGroups.ctl"

#uses "xmlrpc.ctl"
#uses "xmltcp.ctl"

int HTTP_PORT = 8080;
int HTTPS_PORT = 0;

dyn_string g_resourceList;
int g_resourceId = 0;
mapping g_user;

//--------------------------------------------------------------------------------------------------------------------
main()
{
  //Start the HTTP Server
  if (httpServer(true, HTTP_PORT, HTTPS_PORT) < 0)
  {
   DebugN("ERROR: HTTP-server can't start. --- Check license");
   return;
  }
  
  //Start the XmlRpc Handler
  int id = httpConnect("xmlrpc_server", "/RPC2");          

  //Start XmlTcp Handler

}

//--------------------------------------------------------------------------------------------------------------------
mixed xmlrpc_server(const mixed content, string user, string ip, dyn_string ds1, dyn_string ds2, int connIdx)
{
  DebugTN("xmlrpc", content);
  int ret;  
  string sMethod, sRet;
  dyn_mixed daArgs;
  mixed methodResult;
  mixed xmlResult;
  string cookie = httpGetHeader(connIdx, "Cookie");
  dyn_errClass derr;

  //DebugTN("Ausgabe von RCP Request: ", ip, ds1, ds2, connIdx, content);
  
  //Decode content
  ret = xmlrpcDecodeRequest(content, sMethod, daArgs);

  derr = getLastError();
  if (ret < 0 || dynlen(derr)>=1)
  {
    throwError(derr);
    
    //Output Error
    derr = xmlRpcMakeError(PRIO_SEVERE, ERR_SYSTEM, ERR_PARAMETER, "Error parsing xml-rpc stream", "Method: "+sMethod);
    throwError(derr);

    return xmlrpcReturnFault(derr);
  } 

  //Start own method handler
  methodResult = xmlrpc_handler(user, connIdx, ip, sMethod, daArgs);

  derr = xmlRpcGetErrorFromResult(methodResult); /* Get error from result if error occurred */
  if (dynlen(derr) > 0) //Error occurred
  {
    throwError(derr);
    return makeDynString(xmlrpcReturnFault(derr), "Content-Type: text/xml");
  }
  
  sRet = xmlrpcReturnSuccess(methodResult); //Encode result

  //Compress the result if the other side allows it
  if ( strlen(sRet) > 1024 && strpos(httpGetHeader(connIdx, "Accept-Encoding"),"gzip") >= 0)
  {
    //Return compressed content
    blob b;
    gzip(sRet, b);

    xmlResult = makeDynMixed(b,"Content-Type: text/xml","Content-Encoding: gzip");
  }
  else
  {
    //Return plain content
    xmlResult = makeDynString(sRet, "Content-Type: text/xml");
  }
  //DebugN(xmlResult);
  return xmlResult;
}

//--------------------------------------------------------------------------------------------------------------------
int xmlrpc_users()
{
  dyn_string acitveusers;
  int userNumber;
  dyn_string users = mappingKeys(g_user);
  for ( int i=1; i<=dynlen(users); i++ )
  {
    string user = users[i];
    if ( g_user[user]["time"] >= getCurrentTime() - 60 )
      dynAppend(activeusers, user);
  }
  userNumber = dynlen(activeusers);
  return userNumber;
}

//--------------------------------------------------------------------------------------------------------------------
mixed xmlrpc_handler(string user, string connIdx, string ip, string sMethod, dyn_mixed &asValues)
{     
  DebugN("xmlrpc_handler", user, connIdx, ip, sMethod, asValues); 
  string cookie = httpGetHeader(connIdx, "Cookie");
  string language = getBrowserFitPvssLanguage(connIdx);

  mapping item;
  item["time"] = getCurrentTime();
  item["ipaddr"] = ip; 
  g_user[user] = item;
   
  switch (sMethod)
  {
    case "xoa.getProjectName":        return makeDynString(PROJ);

    case "xoa.getTag":                return xoa_getTag (sMethod, asValues, user, language);
    case "xoa.getTags":               return xoa_getTags (sMethod, asValues, user, language);  
    case "xoa.waitForTags":           return xoa_waitForTags (sMethod, asValues, user, language);
      
    case "xoa.getGroups":             return xoa_getGroups (sMethod, asValues, user, language);
    case "xoa.getGroupItems":         return xoa_getGroupItems (sMethod, asValues, user, language);

    case "xoa.addClient":             return xoa_addClient(sMethod, asValues, user, language, ip); 
    case "xoa.delClient":             return xoa_delClient(sMethod, asValues, user, language, ip);
      
    case "xoa.dpQueryConnectSingle": 
    case "xoa.tagQueryConnectSingle": return xoa_dpQueryConnectSingle(sMethod, asValues, user, language); 
    
    case "xoa.dpQueryDisconnect":    
    case "xoa.tagQueryDisconnect":    return xoa_dpQueryDisconnect(sMethod, asValues, user, language);       
      
    case "xoa.dpConnect":            
    case "xoa.tagConnect":            return xoa_dpConnect(sMethod, asValues, user, language); 
      
    case "xoa.dpDisconnect":         
    case "xoa.tagDisconnect":         return xoa_dpDisconnect(sMethod, asValues, user, language);        

    default:
      return methodHandlerCommon(sMethod, asValues, user, cookie);
  }

}

