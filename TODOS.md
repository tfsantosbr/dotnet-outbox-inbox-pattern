# TODOS

1. Arrumar o o mapeamento de colunas do outbox_message para seguit o padral camel_case igual ao inbox_messages
   1. Corrigir migrations
   2. Corrigir as queries sql raw que fazem chamadas nessas tabelas.
2. Adicionar validação nas configurações do inbox/outbox consumers
3. Criar uma validação no OrderCreatedConsumer.cs para que valide se o productId existe, se não existir deve mandar a mensagem para uma dead-letter queue
    