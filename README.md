# AOP
今天分享两种自定义实现AOP方式给大家。
## 一、Emit
利用反射，并结合Emit代码织入方式

## 二、透明代理
使用.net remoting来获取代理访问实例。RealProxy 类提供基本代理功能。它是一个抽象类，必须通过重写其 Invoke 方法并添加新功能来继承。该类在命名空间 System.Runtime.Remoting.Proxies 中。
