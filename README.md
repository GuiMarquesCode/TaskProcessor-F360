# F360 — Serviço de Processamento de Tarefas Assíncronas (C# / .NET)

Solução desenvolvida para o **Desafio Técnico de Programação Back-End C# / ASP.NET da F360**, contemplando uma arquitetura limpa, escalável e desacoplada para recebimento, enfileiramento e processamento em segundo plano de tarefas (jobs) com controle de concorrência e sistema de re-tentativas.

---

## Sumário

1. [Arquitetura e Estrutura da Solution](#1-arquitetura-e-estrutura-da-solution)
2. [Pilares de POO, SOLID e Design Patterns Aplicados](#2-pilares-de-poo-solid-e-design-patterns-aplicados)
3. [Controle de Concorrência e Sistema de Re-tentativas](#3-controle-de-concorrência-e-sistema-de-re-tentativas)
4. [Como Executar o Projeto (Docker Compose)](#4-como-executar-o-projeto-docker-compose)
5. [Como Testar os Cenários na Apresentação (Swagger)](#5-como-testar-os-cenários-na-apresentação-swagger)
6. [Executando os Testes de Unidade (xUnit + Moq)](#6-executando-os-testes-de-unidade-xunit--moq)

---

## 1. Arquitetura e Estrutura da Solution

O projeto utiliza **Clean Architecture enxuta** dividida em responsabilidades claras:

```text
F360.TaskProcessor/
├── docker-compose.yml                         # Orquestração de API, Worker, MongoDB e RabbitMQ
├── src/
│   ├── F360.TaskProcessor.Domain/             # Entidade rica (JobTask), Enums (TaskItemStatus) e Contrato ITaskRepository
│   ├── F360.TaskProcessor.Application/        # Casos de Uso, DTOs, Strategy Pattern, Factory Pattern e Orquestrador
│   ├── F360.TaskProcessor.Infrastructure/     # Persistência NoSQL (MongoDB.Driver) e Mensageria (RabbitMQ.Client)
│   ├── F360.TaskProcessor.Api/                # Web API ASP.NET Core + Swagger + Dockerfile
│   └── F360.TaskProcessor.Worker/             # Worker Service em Background (Consumer RabbitMQ) + Dockerfile
└── tests/
    └── F360.TaskProcessor.UnitTests/          # Testes de Unidade com xUnit e Moq
```

---

## 2. Pilares de POO, SOLID e Design Patterns Aplicados

### Pilares de POO & Composição sobre Herança
- **Encapsulamento**: A entidade de domínio `JobTask` protege todas as suas propriedades com `private set`. Nenhuma classe externa pode alterar o `Status` ou `Attempts` arbitrariamente; todas as transições (`MarkAsProcessing()`, `MarkAsCompleted()`, `RegisterFailure()`) são governadas pela própria entidade.
- **Abstração & Polimorfismo**: A interface `ITaskExecutionStrategy` abstrai o comportamento de processamento. O `TaskProcessingOrchestrator` executa `strategy.ExecuteAsync(task)` de forma polimórfica sem conhecer a classe concreta (`SendEmailTaskStrategy` ou `GenerateReportTaskStrategy`).
- **Composição sobre Herança**: Em vez de acoplar os processadores de tarefas a uma hierarquia rígida de herança, as estratégias recebem comportamentos transversais via injeção de dependência de interfaces segregadas (`ITaskSimulationHelper` e `ILogger`), facilitando testes unitários e manutenção.

### Princípios SOLID
- **S (Single Responsibility Principle)**: Cada classe tem uma responsabilidade única (`JobTask` gerencia regras de estado da tarefa; `MongoTaskRepository` persiste no MongoDB; `RabbitMqPublisher` publica na fila; cada `Strategy` executa apenas 1 tipo de tarefa).
- **O (Open/Closed Principle)**: Para suportar um novo tipo de tarefa (ex.: `"IntegrarPagamento"`), basta criar uma nova classe que implemente `ITaskExecutionStrategy` e registrá-la no DI. Nenhuma linha da `TaskStrategyFactory` ou do `Worker` precisa ser modificada.
- **L (Liskov Substitution Principle)**: Qualquer implementação de `ITaskExecutionStrategy` substitui perfeitamente o contrato esperado pelo orquestrador.
- **I (Interface Segregation Principle)**: Interfaces granulares e coesas (`ITaskRepository`, `IMessageQueuePublisher`, `ITaskExecutionStrategy`, `ITaskStrategyFactory`, `ITaskSimulationHelper`).
- **D (Dependency Inversion Principle)**: As camadas `Application`, `Api` e `Worker` dependem apenas de abstrações (`Interfaces`), nunca diretamente dos drivers concretos de infraestrutura.

### Design Patterns
- **Strategy Pattern**: `ITaskExecutionStrategy` (`SendEmailTaskStrategy`, `GenerateReportTaskStrategy`).
- **Factory Pattern**: `TaskStrategyFactory` resolvendo a estratégia via Injeção de Dependência (`IEnumerable<ITaskExecutionStrategy>`).
- **Repository Pattern**: `ITaskRepository` / `MongoTaskRepository`.
- **Competing Consumers Pattern**: Workers consumindo do RabbitMQ com `BasicQos(prefetchCount: 1)`.

---

## 3. Controle de Concorrência e Sistema de Re-tentativas

### Controle de Concorrência (Requisito 2.c)
A solução implementa **dupla proteção contra concorrência** para que múltiplos workers possam operar simultaneamente sem conflitos:
1. **Na Fila (RabbitMQ)**: Configuração de `BasicQos(prefetchCount: 1)` e `autoAck: false`. O RabbitMQ distribui as mensagens de forma justa e aguarda confirmação manual (`BasicAck`).
2. **No Banco de Dados (MongoDB Atomic Compare-and-Swap)**: O método `TryAcquireForProcessingAsync` utiliza `FindOneAndUpdateAsync` filtrando simultaneamente por `Id == taskId` **E** `Status == Pendente`, alterando atomicamente o status para `EmProcessamento` e incrementando `Attempts`. Se outro worker tentar capturar a mesma tarefa no mesmo milissegundo, o MongoDB retorna `null` para o segundo worker, evitando execução duplicada.

### Sistema de Re-tentativas (Requisito 2.b)
Quando ocorre uma exceção no processamento:
1. A entidade `JobTask` invoca `RegisterFailure(ex.Message)`.
2. Se `Attempts < MaxAttempts`: o status retorna para `Pendente` e a tarefa é re-publicada na fila do RabbitMQ para nova tentativa.
3. Se `Attempts >= MaxAttempts`: o status muda definitivamente para `Erro`, armazena o motivo em `UltimoErro` e encerra as tentativas.

---

## 4. Como Executar o Projeto (Docker Compose)

### Pré-requisitos
- [Docker](https://www.docker.com/) e Docker Compose instalados (ou .NET 10 SDK para execução local).

### Subindo todo o ambiente com 1 comando
Na raiz do projeto, execute:

```bash
docker compose up --build
```

Serviços disponibilizados:
- **Swagger UI (API)**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Painel do RabbitMQ**: [http://localhost:15672](http://localhost:15672) (usuário: `guest` / senha: `guest`)
- **MongoDB**: `mongodb://localhost:27017`

*(Opcional)* Para demonstrar múltiplos workers rodando simultaneamente:
```bash
docker compose up --build --scale worker=3
```

---

## 5. Como Testar os Cenários na Apresentação (Swagger)

Acesse `http://localhost:5000/swagger` e utilize os endpoints:

### Cenário 1: Tarefa processada com Sucesso (`Pendente` -> `EmProcessamento` -> `Concluido`)
`POST /api/tasks`
```json
{
  "tipo": "EnviarEmail",
  "dados": {
    "destinatario": "financeiro@f360.com.br",
    "assunto": "Fechamento de Caixa Diário",
    "tempoSimulacaoMs": 5000
  },
  "maxTentativas": 3
}
```
> Copie o `id` retornado e consulte `GET /api/tasks/{id}` imediatamente para ver o status `"EmProcessamento"`, e novamente após 5 segundos para ver o status `"Concluido"`.

### Cenário 2: Falha e Sistema de Re-tentativas (`Pendente` -> 3 tentativas -> `Erro`)
`POST /api/tasks`
```json
{
  "tipo": "GerarRelatorio",
  "dados": {
    "empresa": "Loja Varejo F360",
    "periodo": "2026-11",
    "simularFalha": true,
    "tempoSimulacaoMs": 2000
  },
  "maxTentativas": 3
}
```
> Ao enviar `"simularFalha": true`, o Worker tentará processar a tarefa 3 vezes (incrementando `tentativasRealizadas` a cada tentativa). Ao atingir o limite `maxTentativas` (3), o status em `GET /api/tasks/{id}` ficará permanentemente como `"Erro"` com os detalhes em `ultimoErro`.

---

## 6. Executando os Testes de Unidade (xUnit + Moq)

Para rodar a suíte de testes automatizados localmente:

```bash
dotnet test F360.TaskProcessor.sln --verbosity normal
```
