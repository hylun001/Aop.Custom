namespace Aop.Emit
{
    public interface IInterceptor
    {
        /// <summary>
        /// 方法调用前
        /// </summary>
        /// <param name="operationName">方法名</param>
        /// <param name="inputs">参数</param>
        /// <returns>状态对象，用于调用后传入</returns>
        object BeforeCall(string operationName, object[] inputs);

        /// <summary>
        /// 方法调用后
        /// </summary>
        /// <param name="operationName">方法名</param>
        /// <param name="returnValue">结果</param>
        /// <param name="correlationState">状态对象</param>
        void AfterCall(string operationName, object returnValue, object correlationState);
    }
}
