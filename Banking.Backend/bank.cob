       IDENTIFICATION DIVISION.
       PROGRAM-ID. BANK-BACKEND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ACCOUNT-BALANCE          PIC 9(9)V99 VALUE 5000.00.
       01 WS-INPUT                 PIC X(120).
       01 ACTION-NAME              PIC X(20).
       01 AMOUNT-TEXT              PIC X(40).
       01 REQUEST-AMOUNT           PIC 9(9)V99.
       01 AMOUNT-OUTPUT            PIC 9(9).99.
       01 BALANCE-OUTPUT           PIC 9(9).99.
       01 TRANSACTION-COUNT        PIC 9(4) COMP VALUE ZERO.
       01 TRANSACTION-TABLE.
          05 TRANSACTION-ROW OCCURS 1000 TIMES.
             10 TRANSACTION-TYPE   PIC X(8).
             10 TRANSACTION-AMOUNT PIC 9(9)V99.
             10 TRANSACTION-BALANCE PIC 9(9)V99.
       01 ROW-INDEX                PIC 9(4) COMP.
       01 END-OF-INPUT             PIC X VALUE "N".
       01 VALID-AMOUNT             PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
           DISPLAY "START|" BALANCE-OUTPUT
           PERFORM UNTIL END-OF-INPUT = "Y"
               MOVE SPACES TO WS-INPUT
               ACCEPT WS-INPUT
               IF WS-INPUT = SPACES
                   DISPLAY "ERROR|EMPTY_COMMAND"
               ELSE
                   PERFORM PROCESS-COMMAND
               END-IF
           END-PERFORM
           STOP RUN.

       PROCESS-COMMAND.
           MOVE SPACES TO ACTION-NAME AMOUNT-TEXT
           UNSTRING WS-INPUT DELIMITED BY "|"
               INTO ACTION-NAME AMOUNT-TEXT
           END-UNSTRING
           EVALUATE FUNCTION TRIM(ACTION-NAME)
               WHEN "BALANCE"
                   PERFORM DISPLAY-BALANCE
               WHEN "DEPOSIT"
                   MOVE "DEPOSIT" TO ACTION-NAME
                   PERFORM VALIDATE-AMOUNT
                   IF VALID-AMOUNT = "Y"
                       PERFORM DEPOSIT-MONEY
                   ELSE
                       IF VALID-AMOUNT = "F"
                           DISPLAY "RESULT|HISTORY_FULL"
                       ELSE
                           DISPLAY "RESULT|INVALID_AMOUNT"
                       END-IF
                   END-IF
               WHEN "WITHDRAW"
                   MOVE "WITHDRAW" TO ACTION-NAME
                   PERFORM VALIDATE-AMOUNT
                   IF VALID-AMOUNT = "Y"
                       PERFORM WITHDRAW-MONEY
                   ELSE
                       IF VALID-AMOUNT = "F"
                           DISPLAY "RESULT|HISTORY_FULL"
                       ELSE
                           DISPLAY "RESULT|INVALID_AMOUNT"
                       END-IF
                   END-IF
               WHEN "HISTORY"
                   PERFORM DISPLAY-HISTORY
               WHEN "EXIT"
                   MOVE "Y" TO END-OF-INPUT
                   DISPLAY "RESULT|EXIT"
               WHEN OTHER
                   DISPLAY "ERROR|UNKNOWN_COMMAND"
           END-EVALUATE.

       VALIDATE-AMOUNT.
           MOVE "N" TO VALID-AMOUNT
           IF FUNCTION TRIM(AMOUNT-TEXT) NOT = SPACES
               IF FUNCTION TEST-NUMVAL(
                   FUNCTION TRIM(AMOUNT-TEXT)) = ZERO
                   IF FUNCTION NUMVAL(AMOUNT-TEXT) > ZERO
                       IF FUNCTION NUMVAL(AMOUNT-TEXT)
                           <= 999999999.99
                           IF TRANSACTION-COUNT < 1000
                           MOVE FUNCTION NUMVAL(AMOUNT-TEXT)
                               TO REQUEST-AMOUNT
                               MOVE "Y" TO VALID-AMOUNT
                           ELSE
                               MOVE "F" TO VALID-AMOUNT
                           END-IF
                       END-IF
                   END-IF
               END-IF
           END-IF.

       DEPOSIT-MONEY.
           ADD REQUEST-AMOUNT TO ACCOUNT-BALANCE
               ON SIZE ERROR
                   MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
                   DISPLAY "RESULT|BALANCE_LIMIT|"
                       BALANCE-OUTPUT
                   EXIT PARAGRAPH
           END-ADD
           PERFORM RECORD-TRANSACTION
           MOVE REQUEST-AMOUNT TO AMOUNT-OUTPUT
           MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
           DISPLAY "RESULT|OK|DEPOSIT|" AMOUNT-OUTPUT "|"
               BALANCE-OUTPUT.

       WITHDRAW-MONEY.
           IF REQUEST-AMOUNT > ACCOUNT-BALANCE
               MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
               DISPLAY "RESULT|INSUFFICIENT_FUNDS|"
                   BALANCE-OUTPUT
           ELSE
               SUBTRACT REQUEST-AMOUNT FROM ACCOUNT-BALANCE
                   ON SIZE ERROR
                       DISPLAY "RESULT|INVALID_AMOUNT"
                       EXIT PARAGRAPH
               END-SUBTRACT
               PERFORM RECORD-TRANSACTION
               MOVE REQUEST-AMOUNT TO AMOUNT-OUTPUT
               MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
               DISPLAY "RESULT|OK|WITHDRAW|" AMOUNT-OUTPUT "|"
                   BALANCE-OUTPUT
           END-IF.

       RECORD-TRANSACTION.
           ADD 1 TO TRANSACTION-COUNT
           MOVE ACTION-NAME TO
               TRANSACTION-TYPE(TRANSACTION-COUNT)
           MOVE REQUEST-AMOUNT TO
               TRANSACTION-AMOUNT(TRANSACTION-COUNT)
           MOVE ACCOUNT-BALANCE TO
               TRANSACTION-BALANCE(TRANSACTION-COUNT).

       DISPLAY-BALANCE.
           MOVE ACCOUNT-BALANCE TO BALANCE-OUTPUT
           DISPLAY "BALANCE|" BALANCE-OUTPUT.

       DISPLAY-HISTORY.
           DISPLAY "HISTORY|" TRANSACTION-COUNT
           PERFORM VARYING ROW-INDEX FROM 1 BY 1
               UNTIL ROW-INDEX > TRANSACTION-COUNT
               MOVE TRANSACTION-AMOUNT(ROW-INDEX)
                   TO AMOUNT-OUTPUT
               MOVE TRANSACTION-BALANCE(ROW-INDEX)
                   TO BALANCE-OUTPUT
               DISPLAY "ITEM|"
                   FUNCTION TRIM(TRANSACTION-TYPE(ROW-INDEX))
                   "|" AMOUNT-OUTPUT "|" BALANCE-OUTPUT
           END-PERFORM
           DISPLAY "END".
