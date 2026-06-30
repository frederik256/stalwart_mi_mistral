hmailserver on windows to stalwart server
c# 10, .net core, cli tool. 
Runs on windows and linux. 
Stalwart runs in a docker container
Stalwart documentation is at https://stalw.art/docs/install/
Stalwart source is at https://github.com/stalwartlabs/stalwart
Stalwart api docs is at https://github.com/stalwartlabs/stalwart/blob/main/api/v1/openapi.yml
Hmailserver is at https://github.com/hmailserver/hmailserver/tree/master

functional goals: 
migrate the email
migrate the user accounts (without passwords). 
support transferring domain by domain, and all domains.
transfer incrementally (ie resume a failed or partial transfer)
migrate via the cli application, not direct via datagbase
support for import into stalwart server running in a linux docker container, hosted on a windows machine. 

Architectural approach:
extract the emails, account data to zip file(s)
import via this cli tool. 
 
