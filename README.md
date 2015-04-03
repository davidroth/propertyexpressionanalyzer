# Roslyn code analyzer and refactoring for property-expression to nameof conversion

A custom refactoring that transforms common property-expression helper code to the new c# 6 nameof expression:

![alt](http://www.fusonic.net/en/assets/images/blog/roslyn-refactoring/diagnostic-preview.png)

If you have some similar form of expresison helper and also want to upgrade your solution to use the new nameof() feature, you can download and modify the code so that it matches your helper classes. 

Most probably you just need to rename the 2 string variables which represent the name of the utility class (ex. "PropertyUtil") and the method (ex: "GetName").
