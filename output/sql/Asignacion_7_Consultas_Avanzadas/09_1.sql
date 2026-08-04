-- EJERCICIO 9.1
SET SERVEROUTPUT ON
DECLARE
  v_apellido employees.last_name%TYPE;
  v_id       employees.employee_id%TYPE;
BEGIN
  SELECT last_name, employee_id
    INTO v_apellido, v_id
    FROM employees
   WHERE last_name = 'Cambrault'
     AND employee_id = (SELECT MIN(employee_id)
                          FROM employees
                         WHERE last_name = 'Cambrault');

  DBMS_OUTPUT.PUT_LINE('Apellido del empleado: ' || v_apellido);
  DBMS_OUTPUT.PUT_LINE('ID del empleado: ' || v_id);
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    DBMS_OUTPUT.PUT_LINE('No se encontro el empleado Cambrault.');
  WHEN TOO_MANY_ROWS THEN
    DBMS_OUTPUT.PUT_LINE('Se encontro mas de un empleado Cambrault.');
  WHEN OTHERS THEN
    DBMS_OUTPUT.PUT_LINE(SQLERRM);
END;
/
