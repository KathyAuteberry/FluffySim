using System.Linq;
using Mono.Cecil;
using Xunit;
using Assert = Xunit.Assert;

namespace AnoMech.SafetyTests.Architecture;

public class NativeDivePresentationTests
{
    [Fact]
    public void RetainedRendererVisibilityAndActionEffectsReachTheClientBoundary()
    {
        foreach (var il in PluginIL.Builds)
        {
            var visibility = il.Method("AnoMech.Core.Native.Implementations.BattleCharaProxy", "set_IsDrawObjectVisible");
            Assert.Contains(visibility.Body.Instructions, i => i.Operand is MethodReference m
                && m.DeclaringType.Namespace.StartsWith("FFXIVClientStructs.") && m.Name == "set_IsVisible");
            Assert.DoesNotContain(visibility.Body.Instructions, i => i.Operand is FieldReference f && f.Name == "RenderFlags");
            var ready = il.Method("AnoMech.Core.Native.Implementations.BattleCharaProxy", "get_IsAnimationReady");
            Assert.Contains(ready.Body.Instructions, i => i.Operand is FieldReference f && f.Name == "Parent");
            var dispatch = il.Method("AnoMech.Core.Native.Implementations.BattleCharaProxy", "ReceiveActionEffect");
            Assert.Contains(dispatch.Body.Instructions, i => i.Operand is MethodReference m
                && m.DeclaringType.Name == "ActionEffectHandler" && m.Name == "Receive");
        }
    }
}
