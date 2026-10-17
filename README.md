# Assistente de Conversas

Aplicação local em **ASP.NET Core 10 + Blazor** para transformar conversas comerciais coladas manualmente em leads, mantendo atualizações e follow-ups por meio de um assistente de chat.

A IA sugere; o usuário confirma. Nada é salvo sem revisão.

## Funcionalidades

- **Analisar conversa** (`/analisar`): cola um texto, a IA extrai os dados do lead em campos editáveis e você salva.
- **Assistente de leads** (`/chat`): conversa em linguagem natural para cadastrar leads, atualizar campos, agendar follow-ups e registrar interações. Cada ação aparece como proposta, com os campos que vão mudar, e só é gravada após **Confirmar**.
- **Leads** (`/leads`): busca, lista com o próximo follow-up (em vermelho quando está atrasado), detalhes editáveis e histórico de interações.
- **Exportação** para CSV, de todos os leads ou de um lead.

## Privacidade

- A entrada é somente o texto que você cola ou digita. A aplicação não acessa o LinkedIn, não faz scraping e não envia mensagens.
- Os dados ficam em um banco SQLite local (`AssistenteConversas/data/leads.db`), que não é versionado.
- O texto colado é enviado à API configurada (Poe) apenas para análise. Use dados fictícios ou autorizados ao testar.

## Tecnologias

- .NET 10 / ASP.NET Core com componentes Blazor (Interactive Server)
- Entity Framework Core 10 com SQLite
- Bootstrap para a interface
- API do Poe (compatível com `chat/completions`) como provedor de IA, com um modo mock para desenvolvimento sem chave

## Como executar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
cd AssistenteConversas
dotnet run
```

Abra o endereço exibido no terminal. Em `Development`, o banco é criado automaticamente na primeira execução.

### Configurar a chave de IA

Por padrão, `appsettings.json` usa o provedor `poe`. Sem chave, a análise e o chat não funcionam. Para guardar a chave fora do código, use o *user secrets*:

```bash
cd AssistenteConversas
dotnet user-secrets set "AI:ApiKey" "SUA_CHAVE_AQUI"
```

Alternativamente, defina a variável de ambiente `POE_API_KEY`.

### Rodar sem chave (modo mock)

Para explorar a interface sem chamar a API, use o provedor mock:

```bash
AI__Provider=Mock dotnet run
```

No modo mock, o assistente responde com uma mensagem fixa e nenhuma ação é proposta.

### Configurações principais

Em `AssistenteConversas/appsettings.json`, na seção `AI`:

| Chave | Descrição |
|---|---|
| `Provider` | `poe` para usar a API, qualquer outro valor usa o mock |
| `Model` | Modelo usado pelo Poe (ex.: `claude-haiku-4-5`) |
| `BaseUrl` | Endereço base da API |
| `Temperature` | Temperatura de geração (padrão baixo, para respostas estáveis) |
| `MaxTokens` | Limite de tokens por resposta |

## Dados de exemplo

A pasta `AssistenteConversas/exemplos/` tem dados totalmente fictícios:

- `conversas-ficticias.txt`: conversas para colar em `/analisar` ou conversar no `/chat`.
- `leads-ficticios.json`: leads de exemplo com os mesmos campos do modelo `Lead`, para referência. Não é importado automaticamente; use-o para preencher manualmente ou como base para um script de carga.

## Como a IA é usada (resumo técnico)

1. **Análise de conversa:** um prompt com contrato JSON fixo pede os campos do lead. Campos ausentes devem vir como `null`.
2. **Assistente de chat:** o modelo recebe a data de hoje e um resumo dos leads cadastrados, e devolve uma resposta e, opcionalmente, uma ação proposta (`criar`, `atualizar` ou `nenhuma`).
3. **Validação no servidor:** temperatura e status são aceitos somente dentro de listas fechadas; datas precisam estar em `aaaa-mm-dd`; um lead só pode ser atualizado se o id existir na lista enviada ao modelo; avisos são mostrados ao usuário quando algo é descartado.
4. **Confirmação:** somente `LeadChangeApplier` grava alterações, e só é chamado quando o usuário clica em **Confirmar**.

## Estrutura

```
AssistenteConversas/
├── Components/Pages/     Páginas Blazor (Analisar, Chat, Leads, Detalhes, Home)
├── Data/                 DbContext do EF Core
├── Migrations/           Migrações do banco
├── Models/               Lead, LeadInteracao, propostas do assistente
├── Prompts/              Prompts enviados ao modelo
├── Services/             Serviços de IA, leads, aplicação de ações e CSV
└── exemplos/             Dados fictícios
```

## Licença

Defina a licença antes de publicar.
