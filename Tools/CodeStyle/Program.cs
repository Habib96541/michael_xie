using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string[] files = Directory.GetFiles(Path.Combine(root,"Assets/Scripts/MineGrid"),"*.cs",SearchOption.AllDirectories).Concat(Directory.GetFiles(Path.Combine(root,"Assets/Editor"),"*.cs",SearchOption.AllDirectories)).ToArray();
var trees=files.Select(f=>CSharpSyntaxTree.ParseText(File.ReadAllText(f),new CSharpParseOptions(preprocessorSymbols:new[]{"UNITY_EDITOR"}),f)).ToArray();
var refs = new List<MetadataReference>();
string unity="C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/Managed";
foreach(string folder in new[]{unity,Path.Combine(unity,"UnityEngine"),Path.Combine(root,"Library/ScriptAssemblies")})
foreach(string file in Directory.GetFiles(folder,"*.dll"))
{
    if(Path.GetFileName(file).StartsWith("Assembly-CSharp"))continue;
    try {refs.Add(MetadataReference.CreateFromFile(file));} catch{}
}
refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
var compilation=CSharpCompilation.Create("Style",trees,refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var fields=new Dictionary<ISymbol,string>(SymbolEqualityComparer.Default);
foreach(var tree in trees)
{
    var model=compilation.GetSemanticModel(tree);
    foreach(var field in tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>())
    {
        bool privateField=!field.Modifiers.Any(t=>t.IsKind(SyntaxKind.PublicKeyword)||t.IsKind(SyntaxKind.InternalKeyword)||t.IsKind(SyntaxKind.ProtectedKeyword)||t.IsKind(SyntaxKind.ConstKeyword));
        if(!privateField)continue;
        foreach(var variable in field.Declaration.Variables)
        {
            var symbol=model.GetDeclaredSymbol(variable);string name=variable.Identifier.ValueText;
            if(symbol!=null&&!name.StartsWith('_'))fields[symbol]="_"+char.ToLowerInvariant(name[0])+name.Substring(1);
        }
    }
}
foreach(var tree in trees)
{
    var rewritten=new StyleRewriter(compilation.GetSemanticModel(tree),fields).Visit(tree.GetRoot());
    File.WriteAllText(tree.FilePath,rewritten.NormalizeWhitespace("    ","\n").ToFullString()+"\n");
}
Console.WriteLine($"Formatted {files.Length} project scripts; renamed {fields.Count} private fields with semantic binding.");

class StyleRewriter(SemanticModel model,Dictionary<ISymbol,string> fields):CSharpSyntaxRewriter
{
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol=model.GetSymbolInfo(node).Symbol;
        return symbol!=null&&fields.TryGetValue(symbol,out string name)?node.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier)):base.VisitIdentifierName(node);
    }
    public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        var symbol=model.GetDeclaredSymbol(node);var result=(VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);
        return symbol!=null&&fields.TryGetValue(symbol,out string name)?result.WithIdentifier(Identifier(name)):result;
    }
    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var members=new List<MemberDeclarationSyntax>();
        foreach(var member in node.Members)
        {
            if(member is FieldDeclarationSyntax field)
            {
                var visited=(FieldDeclarationSyntax)base.VisitFieldDeclaration(field);
                for(int index=0;index<field.Declaration.Variables.Count;index++)
                {
                    var variable=field.Declaration.Variables[index];
                    var result=visited.WithDeclaration(visited.Declaration.WithVariables(SingletonSeparatedList(visited.Declaration.Variables[index])));
                    var symbol=model.GetDeclaredSymbol(variable);
                    if(symbol!=null&&fields.TryGetValue(symbol,out string name))
                    {
                        bool serialized=field.AttributeLists.ToString().Contains("SerializeField");
                        if(serialized)result=result.AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName("UnityEngine.Serialization.FormerlySerializedAs"),AttributeArgumentList(SingletonSeparatedList(AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(variable.Identifier.ValueText)))))))));
                    }
                    if(!HasAccess(result.Modifiers))result=result.WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)).AddRange(result.Modifiers));
                    members.Add(result);
                }
            }
            else
            {
                var result=(MemberDeclarationSyntax)Visit(member);
                if(result is MethodDeclarationSyntax method&&!HasAccess(method.Modifiers))result=method.WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)).AddRange(method.Modifiers));
                if(result is ConstructorDeclarationSyntax ctor&&!HasAccess(ctor.Modifiers)&&!ctor.Modifiers.Any(SyntaxKind.StaticKeyword))result=ctor.WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)).AddRange(ctor.Modifiers));
                members.Add(result);
            }
        }
        return node.WithMembers(List(members));
    }
    public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
    {
        var result=(IfStatementSyntax)base.VisitIfStatement(node);
        if(result.Statement is not BlockSyntax)result=result.WithStatement(Block(result.Statement));
        if(result.Else!=null && result.Else.Statement is not BlockSyntax && result.Else.Statement is not IfStatementSyntax)result=result.WithElse(result.Else.WithStatement(Block(result.Else.Statement)));
        return result;
    }
    public override SyntaxNode VisitForStatement(ForStatementSyntax node)
    {
        var result=(ForStatementSyntax)base.VisitForStatement(node);
        return result.Statement is BlockSyntax?result:result.WithStatement(Block(result.Statement));
    }
    public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
    {
        var result=(ForEachStatementSyntax)base.VisitForEachStatement(node);
        return result.Statement is BlockSyntax?result:result.WithStatement(Block(result.Statement));
    }
    public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
    {
        var result=(WhileStatementSyntax)base.VisitWhileStatement(node);
        return result.Statement is BlockSyntax?result:result.WithStatement(Block(result.Statement));
    }
    static bool HasAccess(SyntaxTokenList tokens)=>tokens.Any(t=>t.IsKind(SyntaxKind.PublicKeyword)||t.IsKind(SyntaxKind.PrivateKeyword)||t.IsKind(SyntaxKind.ProtectedKeyword)||t.IsKind(SyntaxKind.InternalKeyword));
}
