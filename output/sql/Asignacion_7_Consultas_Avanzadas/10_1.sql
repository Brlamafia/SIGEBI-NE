-- EJERCICIO 10.1
SET SERVEROUTPUT ON
CREATE OR REPLACE PROCEDURE actualizar_salario_3
  (p_employee_id IN employees.employee_id%TYPE) IS
  v_apellido employees.last_name%TYPE;
  v_anterior employees.salary%TYPE;
  v_nuevo    employees.salary%TYPE;
BEGIN
  SELECT last_name, salary
    INTO v_apellido, v_anterior
    FROM employees
   WHERE employee_id = p_employee_id;

  DBMS_OUTPUT.PUT_LINE('Antes: ' || v_apellido ||
    ' - ' || TO_CHAR(v_anterior, 'fm$99,999.00'));

  UPDATE employees SET salary = salary * 1.03
   WHERE employee_id = p_employee_id;

  SELECT salary INTO v_nuevo FROM employees
   WHERE employee_id = p_employee_id;

  DBMS_OUTPUT.PUT_LINE('Despues: ' || v_apellido ||
    ' - ' || TO_CHAR(v_nuevo, 'fm$99,999.00'));
END;
/

DEFINE p_employee_id = 100
BEGIN
  actualizar_salario_3(&p_employee_id);
  ROLLBACK;
END;
/
