# Assistente de Conversas

Aplicação local em **ASP.NET Core 10 + Blazor** para cadastrar, atualizar e acompanhar leads por meio de um assistente de chat.

A IA sugere; o usuário confirma. Nada é salvo sem revisão.

## Funcionalidades

- **Assistente de leads** (`/chat`): conversa em linguagem natural para:
  - cadastrar leads novos, com alerta quando já existe um lead parecido;
  - atualizar campos de um lead existente, localizado por nome ou empresa, mesmo com erros de digitação;
  - agendar follow-ups, aceitando datas como "amanhã", "sexta" ou "em 3 dias", e sugerir uma data pelo status do lead;
  - registrar interações no histórico do lead.

  O assistente faz perguntas quando falta alguma informação ou quando há mais de um lead possível. Cada ação aparece como proposta, com os campos que vão mudar, e só é gravada após **Confirmar**.
- **Leads** (`/leads`): busca, lista com o próximo follow-up (em vermelho quando está atrasado), detalhes editáveis e histórico de interações.
- **Exportação** para CSV, de todos os leads ou de um lead.

## Privacidade

- A entrada é somente o texto que você digita ou cola no chat. A aplicação não acessa o LinkedIn, não faz scraping e não envia mensagens.
- Os dados ficam em um banco SQLite local (`AssistenteConversas/data/leads.db`), que não é versionado.
- O texto da conversa é enviado à API configurada (Poe) para gerar as respostas. Use dados fictícios ou autorizados ao testar.

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

Por padrão, `appsettings.json` usa o provedor `poe`. Sem chave, o assistente não responde. Para guardar a chave fora do código, use o *user secrets*:

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

## Rotas

| Rota | Tela |
|---|---|
| `/` | Início |
| `/chat` | Assistente de leads |
| `/leads` | Lista de leads (com filtros) |
| `/leads/{id}` | Detalhes, edição e histórico de interações de um lead |
| `/api/leads/export` | CSV com todos os leads |
| `/api/leads/{id}/export` | CSV de um lead |

## Dados de exemplo

A pasta `AssistenteConversas/exemplos/` tem dados totalmente fictícios:

- `conversas-ficticias.txt`: mensagens para testar o assistente em `/chat`, incluindo um cadastro, uma atualização e um lead novo.
- `leads-ficticios.json`: leads de exemplo com os mesmos campos do modelo `Lead`, para referência. Não é importado automaticamente; use-o para preencher manualmente ou como base para um script de carga.

## Como a IA é usada (resumo técnico)

1. **Protocolo de busca:** o modelo não recebe a lista de leads. Para localizar um lead, ele pede uma busca com nome e empresa. O servidor responde com os candidatos mais parecidos, com tolerância a acento, maiúsculas e pequenos erros de digitação. A busca faz até 3 chamadas por mensagem.
2. **Rascunho da intenção:** o que já foi entendido (tipo da ação, lead selecionado, campos e interação) fica guardado entre mensagens. Assim, o usuário pode responder às perguntas aos poucos.
3. **Proposta:** o modelo marca `pronto` quando tem tudo para agir. Só então o servidor monta a proposta. Enquanto isso, nada é criado.
4. **Validação no servidor:** temperatura e status só aceitam valores de listas fechadas; datas relativas são calculadas no código; um lead só pode ser atualizado se o id veio de uma busca nesta conversa; antes de criar, o sistema alerta sobre leads parecidos.
5. **Sugestão de follow-up:** quando a interação ou o status muda e o lead não tem um follow-up futuro, o sistema sugere uma data pelo status (`Services/FollowUpPolicy.cs`). A sugestão vem com um aviso explicando o motivo.
6. **Confirmação:** somente `LeadChangeApplier` grava alterações, e só é chamado quando o usuário clica em **Confirmar**.

## Estrutura

```
AssistenteConversas/
├── Components/Pages/     Páginas Blazor (Chat, Leads, Detalhes, Home)
├── Data/                 DbContext do EF Core
├── Migrations/           Migrações do banco
├── Models/               Lead, LeadInteracao, rascunho e propostas do assistente
├── Prompts/              Prompt do assistente
├── Services/             Cliente Poe, assistente, busca, datas, aplicação de ações e CSV
└── exemplos/             Dados fictícios
```

## Licença

Defina a licença antes de publicar.
