﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class IOperationTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_ForNull_ThrowsArgumentNullException()
            Assert.ThrowsAny(Of ArgumentNullException)(Function() OperationExtensions.GetCorrespondingOperation(Nothing))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_ForGotoBranch_ThrowsInvalidOperationException()
            Dim source = <![CDATA[
Class C
    Sub F
begin:
        For i = 0 To 1
            GoTo begin 'BIND:"GoTo begin"
        Next
    End Sub
End Class
]]>.Value

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim compilation = CreateEmptyCompilation({syntaxTree})

            Dim result = GetOperationAndSyntaxForTest(Of GoToStatementSyntax)(compilation, fileName)
            Dim branch = TryCast(result.operation, IBranchOperation)

            Dim ex = Assert.ThrowsAny(Of InvalidOperationException)(Function() branch.GetCorrespondingOperation())
            Assert.Equal("Invalid branch kind. Finding a corresponding operation requires 'break' or 'continue', " +
                         "but the current branch kind provided is 'GoTo'.", ex.Message)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ForLoopWithExit()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            Exit For 'BIND2:"Exit For"
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_WhileLoopWithContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of WhileBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        While True 'BIND1:"While True"
            Continue While 'BIND2:"Continue While"
        End While
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_DoWhileLoopWithExitAndContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of DoLoopBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        Do 'BIND1:"Do"
            If True
                Exit Do
            Else
                Continue Do 'BIND2:"Continue Do"
            End If
        Loop While True
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ForEachLoopWithExit()
            AssertOuterIsCorrespondingLoopOfInner(Of ForEachBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For Each i In {1,2,3} 'BIND1:"For Each i In {1,2,3}"
            If i = 2
                Exit For 'BIND2:"Exit For"
            End If
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ForEachLoopWithExitAndContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of ForEachBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For Each i In {1,2,3} 'BIND1:"For Each i In {1,2,3}"
            If i Mod 2 = 0
                Exit For
            Else
                Continue For 'BIND2:"Continue For"
            End If
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_NestedLoops()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            For j = 0 To 1
            Next
            Exit For 'BIND2:"Exit For"
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_NestedLoops2()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 
            For j = 0 To 1 'BIND1:"For j = 0 To 1"
                Exit For 'BIND2:"Exit For"
            Next
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_ExitInCase()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
                Exit Select 'BIND2:"Exit Select"
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_NestedSelects()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
                Select Case 2
                    Case 2
                End Select
                Exit Select 'BIND2:"Exit Select"
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_NestedSelects2()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1
            Case 1
                Select Case 2 'BIND1:"Select Case 2"
                    Case 2
                        Exit Select 'BIND2:"Exit Select"
                End Select
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_LoopInSelect()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        Select Case 1
            Case 1
                For i = 0 To 1 'BIND1:"For i = 0 To 1"
                    Exit For 'BIND2:"Exit For"
                Next
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_SelectInLoop()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1
            Select Case 1 'BIND1:"Select Case 1"
                Case 1
                    Exit Select 'BIND2:"Exit Select"
            End Select
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ContinueNestedInIntermediateSelect()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            Select Case 1 
                Case 1
                    Continue For 'BIND2:"Continue For"
            End Select
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ExitButNoLoop_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of ForBlockSyntax, ILoopOperation, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        ' the following loop is just for utilize the common testing method (which expects 2 bindings in source)    
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
        Next

        Exit For 'BIND2:"Exit For"
    End Sub
End Class
]]>.Value, Function(branch) branch.GetCorrespondingLoop())

            Assert.Null(result.corresponding)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_ExitButNoSwitch_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of SelectBlockSyntax, ISwitchOperation, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        ' the following switch is just for utilize the common testing method (which expects 2 bindings in source)
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
        End Select

        Exit Select 'BIND2:"Exit Select"
    End Sub
End Class
]]>.Value, Function(branch) branch.GetCorrespondingSwitch())

            Assert.Null(result.corresponding)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingLoop_ExitButCorrespondingToSwitch_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of ForBlockSyntax, ILoopOperation, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            Select Case 1
                Case 1
                    Exit Select 'BIND2:"Exit Select"
            End Select
        Next
    End Sub
End Class
]]>.Value, Function(branch) branch.GetCorrespondingLoop())

            Assert.Null(result.corresponding)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingSwitch_ExitButCorrespondingToLoop_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of ForBlockSyntax, ILoopOperation, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        Select Case 1
            Case 1
                For i = 0 To 1 'BIND1:"For i = 0 To 1"
                    Exit For 'BIND2:"Exit For"
                Next
        End Select
    End Sub
End Class
]]>.Value, Function(branch) branch.GetCorrespondingSwitch())

            Assert.Null(result.corresponding)
        End Sub

        Private Sub AssertOuterIsCorrespondingLoopOfInner(Of TOuterSyntax As SyntaxNode, TInnerSyntax As SyntaxNode)(source As string)
            Dim result As (expected As IOperation, actual As IOperation)
            result = GetOuterOperationAndCorrespondingInnerOperation(Of TOuterSyntax, ILoopOperation, TInnerSyntax)(source, Function(branch) branch.GetCorrespondingLoop())

            Assert.Equal(result.expected.Syntax, result.actual.Syntax)
        End Sub

        Private Sub AssertOuterIsCorrespondingSwitchOfInner(source As string)
            Dim result As (expected As IOperation, actual As IOperation)
            result = GetOuterOperationAndCorrespondingInnerOperation(Of SelectBlockSyntax, ISwitchOperation, ExitStatementSyntax)(source, Function(branch) branch.GetCorrespondingSwitch())

            Assert.Equal(result.expected.Syntax, result.actual.Syntax)
        End Sub

        Private Function GetOuterOperationAndCorrespondingInnerOperation(Of TOuterSyntax As SyntaxNode, TOuterOp As {Class, IOperation}, TInnerSyntax As SyntaxNode)(
            source As string, findCorresponding As Func(Of IBranchOperation, IOperation)) As (outer As IOperation, corresponding As IOperation)

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim compilation = CreateEmptyCompilation({syntaxTree})

            Dim holder As (operation As IOperation, node As SyntaxNode)
            holder = GetOperationAndSyntaxForTest(Of TOuterSyntax)(compilation, fileName, 1)
            Dim outer = TryCast(holder.operation, TOuterOp)
            holder = GetOperationAndSyntaxForTest(Of TInnerSyntax)(compilation, fileName, 2)
            Dim inner = TryCast(holder.operation, IBranchOperation)

            Dim correspondingOfInner = If(inner IsNot Nothing, findCorresponding(inner), Nothing)

            Return (outer, correspondingOfInner)

        End Function

    End Class

End Namespace
