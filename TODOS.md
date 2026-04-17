# TODOS

## Atualizar README.md da biblioteca do Inbox

Atualizar o README.md das bibliotecas Messaging, Outbox e Inbox

## Geração de mensagens duplicadas

Ao utilizar o teste de estress para 9 milhões de mensagens. Vejo que o OutboxProcessor no orders-api, que se encocntra com 3 réplicas cconfiguradas no docker-compose.yml. Está pegando e publicando muitas mensagens repitidas no rabbitmq.

Eu suspeito que o problema seja no momento de pegar as mensagens da tabela de outbox_messages. Investigar a causa do problema e aplicar um plano para correção.
