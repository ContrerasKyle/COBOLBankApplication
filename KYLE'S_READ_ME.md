### Hello! :D

This is the first version of the banking application with COBOL backend. Here are requirements and steps of how to run the app:



##### **Requirements**

###### 

###### COBOL Itself

Download COBOL in this link:  

https://superbol.eu/en/developers/windows-aio-3.2/



and follow its download instructions



###### COBOL Extension for VS Code

Download the extension here:  

https://marketplace.visualstudio.com/items?itemName=OCamlPro.SuperBOL



and make sure input your credit card number and the 3 funny digits on the back >:D



###### 

##### **Steps**



###### Step 1: Go to the file directory of the app and open Windows Terminal in it.



###### Step 2: Paste this in the terminal to compile the COBOL backend: 

cobc -x -free -o Banking.Backend\\bank-backend.exe Banking.Backend\\bank.cob



###### Step 3: Paste these two lines one at a time in the terminal:

$env:COBOL\_BACKEND\_PATH = (Resolve-Path .\\Banking.Backend\\bank-backend.exe).Path

dotnet run --project .\\Banking.Api\\Banking.Api.csproj --urls http://localhost:5080



###### Step 4: Open a second terminal in the file directory (KEEP THE FIRST ONE OPEN)



###### Step 5: Paste this line into the second terminal and wait:

dotnet run --project .\\Banking.Client\\Banking.Client.csproj



###### A menu should eventually pop up and the application will be running :D







