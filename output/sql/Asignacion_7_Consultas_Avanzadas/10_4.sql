-- EJERCICIO 10.4
CREATE OR REPLACE FUNCTION tax
  (p_salario IN employees.salary%TYPE)
  RETURN NUMBER IS
BEGIN
  RETURN p_salario * 1.04;
END;
/

COLUMN salario_tax FORMAT $99,999.00
SELECT employee_id, tax(salary) AS salario_tax
  FROM employees
 WHERE tax(salary) > (
       SELECT MAX(tax(salary))
         FROM employees
        WHERE department_id = 80)
 ORDER BY tax(salary);
