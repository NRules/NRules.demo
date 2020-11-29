using NRules.Diagnostics;
using Serilog;

namespace NRules.Samples.ClaimsExpert.Service.Infrastructure
{
    internal class RulesEngineLogger
    {
        public RulesEngineLogger(ISessionFactory sessionFactory)
        {
            sessionFactory.Events.RuleFiredEvent += OnRuleFired;
            sessionFactory.Events.LhsExpressionFailedEvent += OnConditionFailedEvent;
            sessionFactory.Events.RhsExpressionFailedEvent += OnActionFailedEvent;
        }

        private void OnRuleFired(object sender, AgendaEventArgs args)
        {
            Log.Information("Rule fired. Rule={0}", args.Rule.Name);
        }

        private void OnConditionFailedEvent(object sender, LhsExpressionErrorEventArgs args)
        {
            Log.Information("Condition evaluation failed. Condition={0}, Message={1}", args.Expression, args.Exception);
        }

        private void OnActionFailedEvent(object sender, RhsExpressionErrorEventArgs args)
        {
            Log.Information("Action evaluation failed. Action={0}, Message={1}", args.Expression, args.Exception);
        }
    }
}